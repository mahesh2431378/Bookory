using BookStoreMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookStoreMVC.Services.IBookService _bookService;
        private readonly IWebHostEnvironment _hostingEnvironment;

        // =================================================================
        // CORRECTED: Consolidated all dependencies into a single constructor
        // =================================================================
        public BooksController(ApplicationDbContext context, BookStoreMVC.Services.IBookService bookService, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _bookService = bookService;
            _hostingEnvironment = hostingEnvironment;
        }

        // =================================================================
        // UPDATED: GET: Books action now with server-side pagination
        // =================================================================
        public async Task<IActionResult> Index(int? categoryId, int? pageNumber)
        {
            // 1. Get categories for the filter sidebar as before.
            ViewBag.Categories = await _bookService.GetCategoriesAsync();

            // 2. Start building the database query. The .AsQueryable() method creates a
            //    query plan but does NOT fetch any data yet. This is very efficient.
            var booksQuery = _context.Books.Include(b => b.Category).AsQueryable();

            // 3. Apply the category filter if one is selected by the user.
            //    This modifies the query plan before it's executed.
            if (categoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);
                // Store the categoryId so the pagination links can keep the filter active.
                ViewData["CategoryId"] = categoryId.Value;
            }

            // 4. Define the page size and determine the current page number.
            //    If no page number is provided in the URL, it defaults to page 1.
            int pageSize = 8; // You can change this to show more or fewer books per page.
            int currentPage = pageNumber ?? 1;

            // 5. Create the paginated list. This is the only point where the database is
            //    actually queried. It efficiently fetches only one page of books.
            //    This requires the PaginatedList.cs class in your Models folder.
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(booksQuery.AsNoTracking(), currentPage, pageSize);

            // 6. Pass the paginated list object to the view.
            return View(paginatedBooks);
        }

        // =================================================================
        // The rest of your controller methods remain unchanged.
        // =================================================================

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _bookService.GetByIdAsync(id.Value);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // GET: Books/Create
        [Authorize(Roles = "ADMIN")]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create(
            [Bind("Title,Description,Price,CategoryId,StockQuantity")] Book book,
            IFormFile file)
        {
            // Check if the file is valid
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "Please upload an image for the book.");
            }

            if (ModelState.IsValid)
            {
                // Define the uploads folder path
                string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "books");
                Directory.CreateDirectory(uploadsFolder); // Ensure the directory exists

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Set the ImageUrl property of the book
                book.ImageUrl = "/images/books/" + uniqueFileName;

                // Add the book to the database
                await _bookService.AddAsync(book);
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is not valid, return the view with the populated categories
            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Price,CategoryId,StockQuantity,ImageUrl")] Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    await _bookService.UpdateAsync(book);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
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
            ViewBag.Categories = _context.Categories.ToList();
            return View(book);
        }

        // GET: Books/Delete/5
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }
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

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}