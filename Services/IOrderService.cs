using BookStoreMVC.Models;
using BookStoreMVC.Controllers;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Defines operations for creating and retrieving orders.
    /// </summary>
    public interface IOrderService
    {
        Task<List<Order>> GetOrdersAsync(int userId, bool isAdmin);
        Task<Order?> GetOrderAsync(int orderId);
        Task<Order> CreateOrderAsync(int userId, CheckoutVm vm);
        Task UpdateStatusAsync(int orderId, OrderStatus status);
    }
}