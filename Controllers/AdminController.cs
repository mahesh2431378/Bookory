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
            // Fetch all orders with related data
            var allOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate) // Sort by date from newest to oldest
                .ToListAsync();

            // Pass statistical data using ViewBag
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalBooks = await _context.Books.CountAsync();
            ViewBag.TotalOrders = allOrders.Count;
            ViewBag.TotalSales = allOrders.Sum(o => o.TotalAmount); // It's better to calculate from the retrieved orders.

            // Return the view with the list of all orders as the model
            return View(allOrders);
        }

        // GET: Admin/Users
        // Displays a list of all customers.
        public async Task<IActionResult> Users()
        {
            // Query the Users table and filter where the Role property is CUSTOMER
            var users = await _context.Users
                                      .Where(u => u.Role == UserRole.CUSTOMER)
                                      .ToListAsync();
            return View(users);
        }
    }
}