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

            // Determine payment status based on method and success
            PaymentStatus status;
            if (method == "COD")
            {
                status = success ? PaymentStatus.PENDING : PaymentStatus.FAILED;
            }
            else
            {
                status = success ? PaymentStatus.COMPLETED : PaymentStatus.FAILED;
            }

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = method,
                PaymentStatus = status,
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // Update order status
            order.Status = success ? OrderStatus.PENDING : OrderStatus.CANCELLED;

            await _context.SaveChangesAsync();
            return payment;
        }

    }
}