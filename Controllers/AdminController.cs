using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Models;
using System.Linq;

namespace BookStoreMVC.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // --- Part 1: Get statistics using efficient, direct database queries ---
            // This logic is from the first code and is more accurate for sales.
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalBooks = await _context.Books.CountAsync();
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            // Get the sum of all completed payments
            var totalCompletedSales = await _context.Payments
                                                 .Where(p => p.PaymentStatus == PaymentStatus.COMPLETED)
                                                 .SumAsync(p => p.Amount);

            // Get the sum of all payments for cancelled orders
            // Assuming your Payment entity has a navigation property or foreign key to an Order entity
            // Get the sum of all payments for cancelled orders
            var cancelledSales = await _context.Payments
                                            .Where(p => p.PaymentStatus == PaymentStatus.COMPLETED &&
                                                      p.Order.Status == OrderStatus.CANCELLED)
                                            .SumAsync(p => p.Amount);

            // Subtract the cancelled sales from the total completed sales
            ViewBag.TotalSales = totalCompletedSales - cancelledSales;

            // --- Part 2: Fetch recent orders to display on the dashboard ---
            // This feature is from the second code and makes the dashboard much more useful.
            // We use .Take(20) as a best practice to avoid loading thousands of orders.
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate)
                .Take(20) // Fetch the 20 most recent orders
                .ToListAsync();

            // Return the view with the list of recent orders as the model
            return View(recentOrders);
        }

        // GET: Admin/Users
        // Displays a list of all customers.
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                                    .Where(u => u.Role == UserRole.CUSTOMER)
                                    .ToListAsync();
            return View(users);
        }
    }
}