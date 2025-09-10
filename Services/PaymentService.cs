using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Implements payment processing using Entity Framework. The payment status and order status are updated based on success flag.
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
            // Validate order belongs to user
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != userId)
            {
                throw new InvalidOperationException("Invalid order.");
            }

            PaymentStatus paystatus;

            if (method == "COD")
            {
                // If already delivered, mark payment as completed
                if (order.Status == OrderStatus.DELIVERED)
                {
                    paystatus = PaymentStatus.COMPLETED;
                }
                else
                {
                    paystatus = PaymentStatus.PENDING;
                }
            }
            else
            {
                // For non-COD methods
                paystatus = success ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;

                // Only update order status if not delivered
                if (order.Status != OrderStatus.DELIVERED)
                {
                    order.Status = success ? OrderStatus.PENDING : OrderStatus.CANCELLED;
                }
            }

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = paystatus,
                PaymentDate = DateTime.UtcNow
            };

            // This is the crucial step: linking the two entities in memory
            order.Payment = payment;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

    }

}