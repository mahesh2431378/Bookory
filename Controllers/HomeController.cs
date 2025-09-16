using Microsoft.AspNetCore.Mvc;
using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredBooks = await _context.Books.Include(b => b.Category)
                                                    .Where(b => b.Category.Name != "Kids") // Exclude kids' books
                                                    .Take(8)
                                                    .ToListAsync();
            return View(featuredBooks);
        }

        // Action for the new "Kids" home page
        public async Task<IActionResult> Kids()
        {
            var kidsBooks = await _context.Books.Include(b => b.Category)
                                                .Where(b => b.Category.Name == "Kids") // Show only kids' books
                                                .Take(8)
                                                .ToListAsync();
            return View(kidsBooks);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUS()
        {
            return View();
        }

        public IActionResult ContactUS()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}