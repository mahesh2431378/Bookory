using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace BookStoreMVC.Models
{
    /// <summary>
    /// Seeds initial data for the database. This includes an admin user, categories and sample books.
    /// </summary>
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed categories if none exist
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Fiction" },
                    new Category { Name = "Non-Fiction" },
                    new Category { Name = "Science" },
                    new Category { Name = "Technology" },
                    new Category { Name = "History" },
                    new Category { Name = "Fantasy" }
                };
                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            // Seed books if none exist
            if (!context.Books.Any())
            {
                var fiction = context.Categories.First(c => c.Name == "Fiction");
                var science = context.Categories.First(c => c.Name == "Science");
                var technology = context.Categories.First(c => c.Name == "Technology");

                var books = new List<Book>
                {
                    new Book
                    {
                        Title = "The Great Gatsby",
                        Description = "A novel by F. Scott Fitzgerald set in the Jazz Age.",
                        Price = 399.99m,
                        CategoryId = fiction.Id,
                        StockQuantity = 10,
                        ImageUrl = "/images/great_gatsby.jpg"
                    },
                    new Book
                    {
                        Title = "A Brief History of Time",
                        Description = "Stephen Hawking\'s landmark book on cosmology.",
                        Price = 499.00m,
                        CategoryId = science.Id,
                        StockQuantity = 8,
                        ImageUrl = "/images/brief_history_of_time.jpg"
                    },
                    new Book
                    {
                        Title = "Clean Code",
                        Description = "A Handbook of Agile Software Craftsmanship by Robert C. Martin.",
                        Price = 599.00m,
                        CategoryId = technology.Id,
                        StockQuantity = 5,
                        ImageUrl = "/images/clean_code.jpg"
                    }
                };
                context.Books.AddRange(books);
                context.SaveChanges();
            }

            // Seed admin user if none exist
            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@bookstore.com",
                    Role = UserRole.ADMIN,
                    PasswordHash = HashPassword("Admin@123")
                };
                context.Users.Add(admin);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Generates a hashed password using a random salt and PBKDF2.
        /// </summary>
        private static string HashPassword(string password)
        {
            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            // store salt and hashed password together, separated by a period
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }
    }
}