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
            var totalUsers = await _context.Users.CountAsync();
            var totalBooks = await _context.Books.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalSales = await _context.Payments.SumAsync(p => p.Amount);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBooks = totalBooks;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalSales = totalSales;
            return View();
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