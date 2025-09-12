using System.ComponentModel.DataAnnotations;

namespace BookStoreMVC.Models
{
    public enum OrderStatus
    {
        PENDING,
        SHIPPED,
        DELIVERED,
        CANCELLED,

        RETURN_REQUESTED, //customer has requested a return for a deliverd order.

        RETURN_SHIPPED,//THE RETURN ITEM HAS BEEN SHIPPED BACK TO THE MERCHANT
        RETURN_DELIVERED// THE RETURN PROCESS IS COMPLETE AND THE REPLACEMENT HAS BEEN DELIVERED.


    }

    public class Order
    {
        

        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.PENDING;
        // Shipping details for checkout. These fields are optional and filled during the checkout process.
        public string? ShippingName { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingZip { get; set; }
        public string? Phone { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }

    }
}