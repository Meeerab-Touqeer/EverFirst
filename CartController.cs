using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventSystem.Data;
using InventSystem.Models;
using System.Security.Claims;

namespace InventSystem.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        // ===============================
        // CART PAGE
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .ThenInclude(p => p!.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var subTotal = cartItems.Sum(c => c.Product!.Price * c.Quantity);
            var tax = subTotal * 0.1m;

            var viewModel = new CartViewModel
            {
                CartItems = cartItems,
                SubTotal = subTotal,
                Tax = tax,
                Total = subTotal + tax
            };

            return View(viewModel);
        }

        // ===============================
        // ADD TO CART
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Json(new { success = false, message = "Please login first" });

            var product = await _context.Products.FindAsync(productId);

            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Product not found" });

            if (product.StockQuantity < quantity)
                return Json(new { success = false, message = "Insufficient stock" });

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;

                if (cartItem.Quantity > product.StockQuantity)
                    return Json(new { success = false, message = "Stock limit exceeded" });
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, message = "Product added to cart", cartCount });
        }

        // ===============================
        // UPDATE CART QUANTITY
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = GetCurrentUserId();

            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (cartItem == null)
                return Json(new { success = false, message = "Cart item not found" });

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else if (quantity > cartItem.Product!.StockQuantity)
            {
                return Json(new { success = false, message = "Stock limit exceeded" });
            }
            else
            {
                cartItem.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cart updated" });
        }

        // ===============================
        // REMOVE FROM CART
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = GetCurrentUserId();

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // ===============================
        // CHECKOUT PAGE
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Cart is empty";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId);

            var viewModel = new CheckoutViewModel
            {
                ShippingAddress = user?.Address ?? "",
                PaymentMethod = "Cash",
                Total = cartItems.Sum(c => c.Product!.Price * c.Quantity) * 1.1m
            };

            ViewBag.CartItems = cartItems;

            return View(viewModel);
        }

      [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
{
    var userId = GetCurrentUserId();
    if (userId == 0)
        return RedirectToAction("Login", "Account");

    var cartItems = await _context.CartItems
        .Include(c => c.Product)
        .Where(c => c.UserId == userId)
        .ToListAsync();

    if (!cartItems.Any())
    {
        TempData["ErrorMessage"] = "Cart is empty";
        return RedirectToAction("Index");
    }

    foreach (var item in cartItems)
    {
        if (item.Product == null || item.Product.StockQuantity < item.Quantity)
        {
            TempData["ErrorMessage"] = $"Insufficient stock for {item.Product?.Name}";
            return RedirectToAction("Index");
        }
    }

    using var transaction = await _context.Database.BeginTransactionAsync();

    var order = new Order
    {
        OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}",
        UserId = userId,
        VendorId = cartItems.First().Product!.VendorId,
        TotalAmount = cartItems.Sum(c => c.Product!.Price * c.Quantity) * 1.1m,
        Status = "Pending",
        PaymentMethod = model.PaymentMethod,
        PaymentStatus = "Pending",
        ShippingAddress = model.ShippingAddress,
        Notes = model.Notes,
        OrderDate = DateTime.Now
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    foreach (var cartItem in cartItems)
    {
        _context.OrderItems.Add(new OrderItem
        {
            OrderId = order.OrderId,
            ProductId = cartItem.ProductId,
            Quantity = cartItem.Quantity,
            UnitPrice = cartItem.Product!.Price,
            TotalPrice = cartItem.Product.Price * cartItem.Quantity
        });

        cartItem.Product.StockQuantity -= cartItem.Quantity;
    }

    _context.Transactions.Add(new Transaction
    {
        TransactionNumber = $"TXN-{DateTime.Now:yyyyMMddHHmmss}",
        OrderId = order.OrderId,
        UserId = userId,
        Amount = order.TotalAmount,
        Type = "Sale",
        Status = "Pending",
        PaymentMethod = model.PaymentMethod,
        Description = $"Order {order.OrderNumber}",
        TransactionDate = DateTime.Now
    });

    _context.CartItems.RemoveRange(cartItems);

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    TempData["SuccessMessage"] = "Order placed successfully";
    return RedirectToAction("OrderDetails", "Customer", new { id = order.OrderId });
}

        // ===============================
        // CART COUNT
        // ===============================
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetCurrentUserId();

            if (userId == 0)
                return Json(new { count = 0 });

            var count = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { count });
        }
    }
}