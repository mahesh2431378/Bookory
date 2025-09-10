using BookStoreMVC.Models;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Defines operations for managing a user's shopping cart.
    /// </summary>
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync(int userId);
        Task AddToCartAsync(int userId, int bookId);
        Task UpdateQuantityAsync(int userId, int cartItemId, int qty);
        Task RemoveAsync(int userId, int cartItemId);
        Task ClearAsync(int userId);
    }
}