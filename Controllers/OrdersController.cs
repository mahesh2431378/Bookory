using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Models;
using System.Security.Claims;

namespace BookStoreMVC.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            IQueryable<Order> orders;
            if (User.IsInRole("ADMIN"))
            {
                // For administrators include user and order items to display details such as item count
                orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book);
            }
            else
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                orders = _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book);
            }
            var list = await orders.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(list);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                        .ThenInclude(b => b.Category)
                .Include(o => o.User)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            // Ensure user has rights
            if (!User.IsInRole("ADMIN") && order.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }
            return View(order);
        }

        // POST: Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Customer-initiated request to return a delivered order.
        /// Only orders with status DELIVERED can be returned.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null)
                return NotFound();
            if (order.Status != OrderStatus.DELIVERED)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStatus.RETURN_REQUESTED;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Customer-initiated cancellation of an order.  
        /// Cancels only if the order isn’t already delivered, cancelled or in the return process.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null)
                return NotFound();

            if (order.Status == OrderStatus.DELIVERED ||
                order.Status == OrderStatus.CANCELLED ||
                order.Status == OrderStatus.RETURN_REQUESTED ||
                order.Status == OrderStatus.RETURN_SHIPPED ||
                order.Status == OrderStatus.RETURN_DELIVERED)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = OrderStatus.CANCELLED;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

    }
}