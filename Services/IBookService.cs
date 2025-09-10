using BookStoreMVC.Models;

namespace BookStoreMVC.Services
{
    
    /// Defines operations for retrieving and managing books and categories.
    /// Introducing this interface allows controllers to depend on an abstraction
    /// rather than the underlying Entity Framework data context.
    /// </summary>
    public interface IBookService
    {
        /// <summary>
        /// Retrieves all book categories.
        /// </summary>
        Task<List<Category>> GetCategoriesAsync();

        /// <summary>
        /// Retrieves a list of books, optionally filtered by category.
        /// </summary>
        Task<List<Book>> GetBooksAsync(int? categoryId = null);

        /// <summary>
        /// Returns a single book by its identifier.
        /// </summary>
        Task<Book?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new book in the data store.
        /// </summary>
        Task AddAsync(Book book);

        /// <summary>
        /// Updates an existing book.
        /// </summary>
        Task UpdateAsync(Book book);

        /// <summary>
        /// Removes a book by its identifier.
        /// </summary>
        Task DeleteAsync(int id);
    }
}