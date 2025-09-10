using BookStoreMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BookStoreMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string firstName, string lastName, DateTime dob, string gender, string phone, string address)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError(string.Empty, "Username already exists.");
                return View();
            }

            // Convert the gender string to the enum type
            if (!Enum.TryParse(gender, true, out Gender userGender))
            {
                ModelState.AddModelError(string.Empty, "Invalid gender provided.");
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dob,
                Gender = userGender,
                Phone = phone,
                Address = address,
                Role = UserRole.CUSTOMER,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Automatically login after registration
            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View();
            }
            // In a real application, you'd generate a token and send an email here.
            TempData["Message"] = "If an account with that email exists, a password reset link has been sent.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View();
            }
            // Sign the user in
            await SignInUser(user);
            // After signing in, redirect based on the user's role.  If the user is an
            // administrator, take them to the admin dashboard; otherwise send
            // them to the normal home page.  This ensures admins see the
            // administrative interface immediately after login instead of the
            // customer storefront.
            if (user.Role == UserRole.ADMIN)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        // Hash password for storing
        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        // Verify password
        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var storedSubKey = parts[1];
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            return hashed == storedSubKey;
        }

        // GET: Account/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            // Get the ID of the currently logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User updatedUser)
        {
            // Get the current user's ID to ensure they can only edit their own profile
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || updatedUser.Id.ToString() != userId)
            {
                return Unauthorized(); // Return an unauthorized status if the user tries to edit another user's profile
            }

            // Explicitly remove properties that shouldn't be changed via this form to prevent overposting attacks.
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");
            // You might also want to remove Username if you don't allow it to be changed
            ModelState.Remove("Username");

            if (ModelState.IsValid)
            {
                var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (userToUpdate == null)
                {
                    return NotFound();
                }

                // Update the properties that are allowed to be changed
                userToUpdate.FirstName = updatedUser.FirstName;
                userToUpdate.LastName = updatedUser.LastName;
                userToUpdate.Email = updatedUser.Email;
                userToUpdate.Phone = updatedUser.Phone;
                userToUpdate.DateOfBirth = updatedUser.DateOfBirth;
                userToUpdate.Gender = updatedUser.Gender;
                userToUpdate.Address = updatedUser.Address;

                try
                {
                    _context.Update(userToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == updatedUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Redirect back to the profile page after a successful update
                return RedirectToAction("Index", "Home");
            }

            // If the model state is not valid, return the view with the updated data to show validation errors
            return View(updatedUser);
        }

        public async Task<IActionResult> Profile()
        {
            // Get the ID of the currently logged-in user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If the userId is not found in the claims, the user is not logged in.
            // Redirect them to the login page.
            if (userId == null)
            {
                // Use a return URL so the user is redirected back to the profile page after login.
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Profile", "User") });
            }

            // Find the user in the database by their ID.
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            // If the user object is null (i.e., user not found in the database),
            // redirect them to the login page to re-authenticate or register.
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Pass the user details to the view
            return View(user);
        }
    }
}