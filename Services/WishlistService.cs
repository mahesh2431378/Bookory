using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Service implementation for managing wishlist items using Entity Framework.
    /// </summary>
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<WishlistItem>> GetWishlistItemsAsync(int userId)
        {
            return await _context.WishlistItems
                .Include(wi => wi.Book)
                    .ThenInclude(b => b.Category)
                .Where(wi => wi.UserId == userId)
                .OrderByDescending(wi => wi.Id)
                .ToListAsync();
        }

        public async Task AddToWishlistAsync(int userId, int bookId)
        {
            // Check if the item already exists to prevent duplicates
            var existing = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.BookId == bookId);

            if (!existing)
            {
                var wishlistItem = new WishlistItem
                {
                    UserId = userId,
                    BookId = bookId
                };
                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromWishlistAsync(int userId, int wishlistItemId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == userId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}