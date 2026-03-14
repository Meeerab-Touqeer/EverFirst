using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventSystem.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public string TransactionNumber { get; set; } = string.Empty;

        public int? OrderId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Sale, Refund, Adjustment

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Pending, Failed

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
