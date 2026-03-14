using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventSystem.Data;
using InventSystem.Models;
using InventSystem.Services;
using System.Security.Claims;

namespace InventSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExportService _exportService;

        public AdminController(ApplicationDbContext context, ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        private bool IsAdmin()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value == "Admin";
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                LowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= p.ReorderLevel && p.IsActive),
                TotalRevenue = await _context.Orders.Where(o => o.PaymentStatus == "Paid").SumAsync(o => o.TotalAmount),
                TodayRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Date == DateTime.Today && o.PaymentStatus == "Paid")
                    .SumAsync(o => o.TotalAmount),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month && 
                               o.OrderDate.Year == DateTime.Now.Year && 
                               o.PaymentStatus == "Paid")
                    .SumAsync(o => o.TotalAmount),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                LowStockProductsList = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.StockQuantity <= p.ReorderLevel && p.IsActive)
                    .Take(10)
                    .ToListAsync(),
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync()
            };

            // Monthly Sales Data (Last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlySales = await _context.Orders
                .Where(o => o.OrderDate >= sixMonthsAgo && o.PaymentStatus == "Paid")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new RevenueByMonth
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:00}",
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            viewModel.RevenueChart = monthlySales;

            // Category Distribution
            var categoryData = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category!.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);

            viewModel.CategoryDistribution = categoryData;

            // Order Status Distribution
            var orderStatusData = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            viewModel.OrderStatusData = orderStatusData;

            // Top Selling Products
            viewModel.TopSellingProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p!.Category)
                .GroupBy(oi => oi.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    TotalSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .Select(x => x.Product!)
                .ToListAsync();

            return View(viewModel);
        }

        // Products Management
        [HttpGet]
        public async Task<IActionResult> Products(string? search, int? categoryId, string? sortBy)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.Products.Include(p => p.Category).Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            query = sortBy switch
            {
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "stock" => query.OrderBy(p => p.StockQuantity),
                "stock_desc" => query.OrderByDescending(p => p.StockQuantity),
                _ => query.OrderBy(p => p.Name)
            };

            var products = await query.ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.SortBy = sortBy;

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                // Check if SKU already exists
                if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
                {
                    ModelState.AddModelError("SKU", "SKU already exists");
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    return View(product);
                }

                product.CreatedAt = DateTime.Now;
                product.IsActive = true;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                // Check if SKU already exists for another product
                if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.ProductId != product.ProductId))
                {
                    ModelState.AddModelError("SKU", "SKU already exists");
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                    return View(product);
                }

                product.UpdatedAt = DateTime.Now;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }

            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> ExportProducts()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var csvData = _exportService.ExportProductsToCsv(products);
            return File(csvData, "text/csv", $"Products_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Categories Management
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                category.IsActive = true;
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction("Categories");
            }

            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category updated successfully!";
                return RedirectToAction("Categories");
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }

            return RedirectToAction("Categories");
        }

        // Orders Management
        [HttpGet]
        public async Task<IActionResult> Orders(string? status, string? search)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) || 
                                        o.User!.FullName!.Contains(search));
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;

                if (status == "Shipped" && order.ShippedDate == null)
                {
                    order.ShippedDate = DateTime.Now;
                }
                else if (status == "Delivered" && order.DeliveredDate == null)
                {
                    order.DeliveredDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Order status updated successfully!";
            }

            return RedirectToAction("OrderDetails", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportOrders()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .ToListAsync();

            var csvData = _exportService.ExportOrdersToCsv(orders);
            return File(csvData, "text/csv", $"Orders_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Transactions
        [HttpGet]
        public async Task<IActionResult> Transactions(string? type, DateTime? startDate, DateTime? endDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.Type == type);
            }

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
            ViewBag.Type = type;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(transactions);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTransactions()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var transactions = await _context.Transactions
                .Include(t => t.Order)
                .Include(t => t.User)
                .ToListAsync();

            var csvData = _exportService.ExportTransactionsToCsv(transactions);
            return File(csvData, "text/csv", $"Transactions_{DateTime.Now:yyyyMMdd}.csv");
        }

        // Customers Management
        [HttpGet]
        public async Task<IActionResult> Customers(string? search)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var query = _context.Users.Where(u => u.Role == "Customer" && u.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) || 
                                        u.Email.Contains(search) || 
                                        u.FullName!.Contains(search));
            }

            var customers = await query.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Search = search;

            return View(customers);
        }

        // Stock Alerts
        [HttpGet]
        public async Task<IActionResult> StockAlerts()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var lowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= p.ReorderLevel && p.IsActive)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            return View(lowStockProducts);
        }
    }
}
