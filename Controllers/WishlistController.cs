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
        public async Task<IActionResult> Add(int bookId, string returnUrl) // Add the returnUrl parameter
        {
            await _wishlistService.AddToWishlistAsync(CurrentUserId, bookId);
            TempData["Message"] = "Book added to your wishlist!";

            // This is the important part: check for a valid return URL and redirect.
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            // Fallback to the default page if the returnUrl is not provided or invalid.
            return RedirectToAction("Index", "Books");
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
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Json(new { count = 0 });
            }

            int userId = int.Parse(userIdClaim);
            var count = await _wishlistService.GetWishlistCountAsync(userId);
            return Json(new { count = count });
        }
    }
}