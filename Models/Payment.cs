using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models
{
    public enum PaymentStatus
    {
        PENDING,
        COMPLETED,
        FAILED
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.PENDING;

        /// <summary>
        /// Indicates the payment method used (e.g., UPI, Card, Cash).
        /// </summary>
        [Required]
        public string PaymentMethod { get; set; } = "UPI";

        [Required]
        public DateTime PaymentDate { get; set; }
    }
}