using Microsoft.AspNetCore.Mvc;

namespace InventSystem.Models
{
    public class Vendor
    {
        public int VendorId { get; set; }
        public string? Name { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; } // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; }
        public string? Password{ get; set; }
        // Navigation
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

}
