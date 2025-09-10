using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Service implementation for managing cart items using Entity Framework.
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            return await _context.CartItems
                .Include(ci => ci.Book)
                    .ThenInclude(b => b.Category)
                .Where(ci => ci.UserId == userId)
                .OrderByDescending(ci => ci.Id)
                .ToListAsync();
        }

        public async Task AddToCartAsync(int userId, int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null || book.StockQuantity <= 0) return;
            var existing = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);
            if (existing == null)
            {
                existing = new CartItem { UserId = userId, BookId = bookId, Quantity = 1 };
                _context.CartItems.Add(existing);
            }
            else
            {
                existing.Quantity += 1;
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int userId, int cartItemId, int qty)
        {
            var item = await _context.CartItems.Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item != null)
            {
                int maxQty = item.Book != null ? item.Book.StockQuantity : qty;
                item.Quantity = Math.Max(1, Math.Min(qty, maxQty));
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveAsync(int userId, int cartItemId)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearAsync(int userId)
        {
            var items = _context.CartItems.Where(ci => ci.UserId == userId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}