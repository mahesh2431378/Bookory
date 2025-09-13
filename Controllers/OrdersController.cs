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
        // This action lists orders. Admins see all orders; users see only their own.
        public async Task<IActionResult> Index()
        {
            IQueryable<Order> orders;
            if (User.IsInRole("ADMIN"))
            {
                // For administrators, include all related data for a comprehensive overview.
                orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payment)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book);
            }
            else
            {
                // For regular users, show only their own orders.
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                orders = _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.Payment)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book);
            }
            var list = await orders.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(list);
        }

        // GET: Orders/Details/5
        // Shows the full details for a single order.
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

            // Security check: Ensure a user can only view their own order unless they are an admin.
            if (!User.IsInRole("ADMIN") && order.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }

            return View(order);
        }

        /// <summary>
        /// (Admin-only) Updates the status of an order.
        /// - If status is set to DELIVERED, payment is marked COMPLETED.
        /// - If status is set to CANCELLED, items are returned to stock.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            // Eagerly load related objects to update them if necessary.
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status; // Store original status for comparison.
            order.Status = status;

            // Handle status-specific side effects.

            // 1. If order is delivered, complete the payment.
            if (status == OrderStatus.DELIVERED)
            {
                if (order.Payment != null)
                {
                    // This correctly updates the status on the related Payment object.
                    order.Payment.PaymentStatus = PaymentStatus.COMPLETED;
                }
            }

            // 2. If order is cancelled, return items to stock.
            // This runs only if the status changes TO Cancelled FROM something else.
            if (status == OrderStatus.CANCELLED && oldStatus != OrderStatus.CANCELLED)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Book != null)
                    {
                        item.Book.StockQuantity += item.Quantity;
                    }
                }
            }

            _context.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// (Customer-facing) Request to return a delivered order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // A return can only be requested for a delivered order.
            if (order.Status != OrderStatus.DELIVERED)
                return RedirectToAction(nameof(Details), new { id });

            order.Status = OrderStatus.RETURN_REQUESTED;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// (Customer-facing) Cancels an order and returns items to stock.
        /// Cancellation is only allowed if the order is not yet delivered or in the return process.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Must include OrderItems and Books to update stock quantity.
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // Define statuses that cannot be cancelled by the user.
            var cancellable = order.Status != OrderStatus.DELIVERED &&
                              order.Status != OrderStatus.CANCELLED &&
                              order.Status != OrderStatus.RETURN_REQUESTED &&
                              order.Status != OrderStatus.RETURN_SHIPPED &&
                              order.Status != OrderStatus.RETURN_DELIVERED;

            if (!cancellable)
            {
                // If not cancellable, just show the details page without making changes.
                return RedirectToAction(nameof(Details), new { id });
            }

            // Return items to stock.
            foreach (var item in order.OrderItems)
            {
                if (item.Book != null)
                {
                    item.Book.StockQuantity += item.Quantity;
                }
            }

            order.Status = OrderStatus.CANCELLED;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}