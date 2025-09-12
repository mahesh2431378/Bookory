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
                // For administrators, include user and order items to display details
                orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Book);
            }
            else
            {
                // For regular users, show only their own orders
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

            // Ensure the user has rights to view the order
            if (!User.IsInRole("ADMIN") && order.UserId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            {
                return Forbid();
            }

            return View(order);
        }

        // POST: Orders/UpdateStatus/5
        /// <summary>
        /// (Admin-only) Updates the status of an order.
        /// If the status is set to DELIVERED, it also updates the payment status to COMPLETED.
        /// If the status is set to CANCELLED, it returns items to stock.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            // Eagerly load related objects to update them if necessary
            var order = await _context.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Store the original status to compare against the new one
            var oldStatus = order.Status;

            // Update the order's status
            order.Status = status;

            // --- NEW LOGIC: Return items to stock if order is cancelled ---
            // This logic runs only if the status is changed TO Cancelled FROM something else.
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

            // **CRITICAL LOGIC**: If the order is delivered, complete the payment status.
            if (status == OrderStatus.DELIVERED)
            {
                if (order.Payment != null)
                {
                    order.Payment.PaymentStatus = PaymentStatus.COMPLETED;
                }
            }

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
        /// When cancelled, returns items to stock.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Must include OrderItems and Books to update stock quantity
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // Check if the order is in a state that allows cancellation
            if (order.Status == OrderStatus.DELIVERED ||
                order.Status == OrderStatus.CANCELLED ||
                order.Status == OrderStatus.RETURN_REQUESTED ||
                order.Status == OrderStatus.RETURN_SHIPPED ||
                order.Status == OrderStatus.RETURN_DELIVERED)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            // --- NEW LOGIC: Return items to stock if order is cancelled ---
            // The check above ensures we don't re-add stock for an already cancelled order.
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