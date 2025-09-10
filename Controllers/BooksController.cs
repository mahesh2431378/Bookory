using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookStoreMVC.Services.IBookService _bookService;

        public BooksController(ApplicationDbContext context, BookStoreMVC.Services.IBookService bookService)
        {
            _context = context;
            _bookService = bookService;
        }

        // =================================================================
        // UPDATED: GET: Books action with Searching, Sorting, Filtering, and Pagination
        // =================================================================
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? categoryId, int? pageNumber)
        {
            // --- Store current state for the view to rebuild links ---
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;

            // --- Get categories for the filter sidebar ---
            ViewBag.Categories = await _bookService.GetCategoriesAsync();

            // --- Start building the database query (doesn't fetch data yet) ---
            var booksQuery = _context.Books.Include(b => b.Category).AsQueryable();

            // --- NEW: Apply Search Filter ---
            if (!string.IsNullOrEmpty(searchString))
            {
                booksQuery = booksQuery.Where(b => b.Title.Contains(searchString));
            }

            // --- Apply Category Filter ---
            if (categoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);
            }

            // --- NEW: Apply Sorting ---
            switch (sortOrder)
            {
                case "name_desc":
                    booksQuery = booksQuery.OrderByDescending(b => b.Title);
                    break;
                case "Price":
                    booksQuery = booksQuery.OrderBy(b => b.Price);
                    break;
                case "price_desc":
                    booksQuery = booksQuery.OrderByDescending(b => b.Price);
                    break;
                default: // Default sort by Title ascending
                    booksQuery = booksQuery.OrderBy(b => b.Title);
                    break;
            }

            // --- Set page size and create the paginated list ---
            int pageSize = 9;
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(booksQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedBooks);
        }

        // =================================================================
        // The rest of the controller methods are below, with minor refactoring for consistency.
        // =================================================================

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();
            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create()
        {
            // REFACTORED: Use service to get categories
            ViewBag.Categories = await _bookService.GetCategoriesAsync();
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create([Bind("Title,Description,Price,CategoryId,StockQuantity,ImageUrl")] Book book)
        {
            if (ModelState.IsValid)
            {
                await _bookService.AddAsync(book);
                return RedirectToAction(nameof(Index));
            }
            // REFACTORED: Use service to get categories
            ViewBag.Categories = await _bookService.GetCategoriesAsync();
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            // REFACTORED: Use service to get book by ID
            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();
            // REFACTORED: Use service to get categories
            ViewBag.Categories = await _bookService.GetCategoriesAsync();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Price,CategoryId,StockQuantity,ImageUrl")] Book book)
        {
            if (id != book.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _bookService.UpdateAsync(book);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // REFACTORED: Use service to get categories
            ViewBag.Categories = await _bookService.GetCategoriesAsync();
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            // REFACTORED: Use service to get book by ID
            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _bookService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // REFACTORED: This should ideally be moved into the IBookService as well.
        private async Task<bool> BookExists(int id)
        {
            return await _bookService.GetByIdAsync(id) != null;
        }
    }
}