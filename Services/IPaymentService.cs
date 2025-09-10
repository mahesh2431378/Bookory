using BookStoreMVC.Models;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Defines operations for processing payments.
    /// </summary>
    public interface IPaymentService
    {
        Task<Payment> ProcessPaymentAsync(int userId, int orderId, decimal amount, string method, bool success);
    }
}