using InventSystem.Data;
using InventSystem.Models;
using InventSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventSystem.Controllers
{
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendorController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentVendorId()
        {
            var vendorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(vendorIdClaim, out var vendorId) ? vendorId : 0;
        }

        // ===============================
        // CREATE PRODUCT
        // ===============================
        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                "CategoryId",
                "Name"
            );
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            var vendorId = GetCurrentVendorId();
            if (vendorId == 0) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                product.VendorId = vendorId;
                product.CreatedAt = DateTime.Now;
                product.IsActive = true;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product added successfully!";
                return RedirectToAction("Products");
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                "CategoryId",
                "Name"
            );
            return View(product);
        }

        // ===============================
        // PLACE ORDER TO VENDOR (FIXED)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int productId, int quantity)
        {
            var vendorId = GetCurrentVendorId();
            if (vendorId == 0) return RedirectToAction("Login");

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Products");
            }

            // Create order record
            var order = new Order
            {
                VendorId = vendorId,
                TotalAmount = product.Price * quantity,
                Status = "Pending",
                OrderDate = DateTime.Now,
                ShippingAddress = "N/A"
            };

            // Add order item
            var orderItem = new OrderItem
            {
                Order = order,
                ProductId = product.ProductId,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * quantity
            };

            order.OrderItems.Add(orderItem);

            _context.Orders.Add(order);

            // ✅ Update product stock
            product.StockQuantity -= quantity;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order placed successfully! Stock reduced by {quantity}.";
            return RedirectToAction("Orders");
        }

        // ===============================
        // VENDOR PRODUCTS
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var vendorId = GetCurrentVendorId();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.VendorId == vendorId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ===============================
        // LOGIN / REGISTER
        // ===============================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VendorRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.Vendors.AnyAsync(v => v.ContactEmail == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var vendor = new Vendor
            {
                Name = model.Name,
                ContactEmail = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful. You can log in now.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(VendorLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.ContactEmail == model.Email);
            if (vendor == null || !BCrypt.Net.BCrypt.Verify(model.Password, vendor.Password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            await SignInVendor(vendor);
            return RedirectToAction("Dashboard");
        }

        // ===============================
        // DASHBOARD / ORDERS
        // ===============================
        public IActionResult Dashboard()
        {
            var vendorId = GetCurrentVendorId();
            var vendor = _context.Vendors
                .Include(v => v.Products)
                .ThenInclude(p => p.OrderItems)
                .FirstOrDefault(v => v.VendorId == vendorId);

            if (vendor == null) return NotFound();

            var orders = _context.Orders.Where(o => o.VendorId == vendorId);

            var dashboard = new VendorDashboardViewModel
            {
                VendorName = vendor.Name,
                TotalOrders = orders.Count(),
                TotalRevenue = orders.Sum(o => (decimal?)o.TotalAmount) ?? 0,
                TotalCommission = orders.Sum(o => (decimal?)(o.TotalAmount * 0.10m)) ?? 0,
                TopProducts = vendor.Products
                    .Select(p => new ProductSalesInfo
                    {
                        Name = p.Name,
                        SalesCount = p.OrderItems.Sum(oi => oi.Quantity)
                    })
                    .OrderByDescending(p => p.SalesCount)
                    .Take(5)
                    .ToList()
            };

            return View(dashboard);
        }

        public async Task<IActionResult> Orders()
        {
            var vendorId = GetCurrentVendorId();
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.VendorId == vendorId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ===============================
        // AUTH HELPER
        // ===============================
        private async Task SignInVendor(Vendor vendor)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, vendor.VendorId.ToString()),
                new Claim(ClaimTypes.Name, vendor.Name ?? ""),
                new Claim(ClaimTypes.Email, vendor.ContactEmail ?? ""),
                new Claim(ClaimTypes.Role, "Vendor")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(3)
                });
        }
    }
}
