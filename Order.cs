using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventSystem.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string? OrderNumber { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? VendorId { get; set; }
        public Vendor Vendor { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        // Navigation
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
