using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BookStoreMVC.Services;

namespace BookStoreMVC.Controllers
{
    [Authorize] // Require users to be logged in to use the wishlist
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /Wishlist
        public async Task<IActionResult> Index()
        {
            var items = await _wishlistService.GetWishlistItemsAsync(CurrentUserId);
            return View(items);
        }

        // POST: /Wishlist/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int bookId)
        {
            await _wishlistService.AddToWishlistAsync(CurrentUserId, bookId);
            TempData["Message"] = "Book added to your wishlist!";
            return RedirectToAction("Index", "Books"); // Redirect back to the book list
        }

        // POST: /Wishlist/Remove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            await _wishlistService.RemoveFromWishlistAsync(CurrentUserId, id);
            TempData["Message"] = "Book removed from your wishlist.";
            return RedirectToAction(nameof(Index));
        }
    }
}