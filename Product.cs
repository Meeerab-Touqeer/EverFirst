using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventSystem.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, StringLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public int ReorderLevel { get; set; } = 10;

        [Required]
        public int CategoryId { get; set; }

        [StringLength(200)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;

        // ✅ Vendor relationship
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        // ✅ Navigation Properties
        public virtual Category? Category { get; set; }

        // Existing navigations
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // ✅ NEW: Direct navigation to Orders
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
