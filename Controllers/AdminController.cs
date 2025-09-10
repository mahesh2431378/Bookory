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
            ViewBag.TotalSales = await _context.Payments
                                                .Where(p => p.PaymentStatus == PaymentStatus.COMPLETED)
                                                .SumAsync(p => p.Amount); // More accurate for actual sales

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