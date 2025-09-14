using BookStoreMVC.Models;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Defines operations for managing a user's wishlist.
    /// </summary>
    public interface IWishlistService
    {
        Task<List<WishlistItem>> GetWishlistItemsAsync(int userId);
        Task AddToWishlistAsync(int userId, int bookId);
        Task RemoveFromWishlistAsync(int userId, int wishlistItemId);
    }
}