using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Implements payment processing using Entity Framework. 
    /// The payment and order statuses are updated based on the payment method and success flag.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> ProcessPaymentAsync(int userId, int orderId, decimal amount, string method, bool success)
        {
            // 1. Validate that the order exists and belongs to the user.
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != userId)
            {
                throw new InvalidOperationException("Invalid order.");
            }

            PaymentStatus paymentStatus;

            // 2. Determine payment and order status based on the method and success flag.
            if (method == "COD")
            {
                // For Cash on Delivery, if the order is already delivered, this action marks it as paid.
                // Otherwise, a new COD order's payment is pending until delivery.
                paymentStatus = (order.Status == OrderStatus.DELIVERED)
                                ? PaymentStatus.COMPLETED
                                : PaymentStatus.PENDING;

                // A new COD order is moved to PENDING for processing.
                if (order.Status != OrderStatus.DELIVERED)
                {
                    order.Status = OrderStatus.PENDING;
                }
            }
            else // For all other online payment methods.
            {
                if (success)
                {
                    paymentStatus = PaymentStatus.COMPLETED;
                    // Protect final states: only update the order status if it's not already completed/delivered.
                    if (order.Status != OrderStatus.DELIVERED)
                    {
                        order.Status = OrderStatus.PENDING;
                    }
                }
                else
                {
                    paymentStatus = PaymentStatus.FAILED;
                    if (order.Status != OrderStatus.DELIVERED)
                    {
                        order.Status = OrderStatus.CANCELLED;
                    }
                }
            }

            // 3. Create the payment record.
            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = paymentStatus,
                PaymentDate = DateTime.UtcNow
            };

            // Explicitly link the navigation property for a consistent object graph.
            order.Payment = payment;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }
    }
}