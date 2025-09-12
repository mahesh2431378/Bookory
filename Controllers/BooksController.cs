using BookStoreMVC.Models;
using BookStoreMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStoreMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly IBookService _bookService;

        public BooksController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IBookService bookService)
        {
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _bookService = bookService;
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
        public async Task<IActionResult> Create([Bind("Title,Description,Price,CategoryId,StockQuantity")] Book book, IFormFile file)
        {
            // Use IWebHostEnvironment to get the wwwroot path
            string wwwRootPath = _webHostEnvironment.WebRootPath;

            // Check if a file was uploaded
            if (file != null)
            {
                // Define the path where the image will be saved
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"images");
                var extension = Path.GetExtension(file.FileName);

                // Save the new image to the uploads folder
                using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                {
                    await file.CopyToAsync(fileStreams);
                }

                // Update the book's ImageUrl with the new path
                book.ImageUrl = @"/images/" + fileName + extension;
            }
            else
            {
                // If no image is uploaded, keep the default image URL
                book.ImageUrl = "/images/default-book.png";
            }

            if (ModelState.IsValid)
            {
                await _bookService.AddAsync(book);
                return RedirectToAction(nameof(Index));
            }

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
            // Find the book to be deleted, including its related OrderItems.
            var book = await _context.Books
                                     .Include(b => b.OrderItems)
                                     .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            // Explicitly delete all OrderItems associated with this book first.
            // This resolves the foreign key constraint conflict.
            if (book.OrderItems != null && book.OrderItems.Any())
            {
                _context.OrderItems.RemoveRange(book.OrderItems);
            }

            // Now, delete the book record.
            _context.Books.Remove(book);

            // Save all changes to the database in a single transaction.
            // EF Core will correctly order the DELETE statements.
            await _context.SaveChangesAsync();

            // Add a success message to display to the user.
            TempData["SuccessMessage"] = "The book was successfully deleted.";

            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}