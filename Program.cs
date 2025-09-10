using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor for injecting HttpContext into views (e.g. _Layout)
builder.Services.AddHttpContextAccessor();

// Configure Entity Framework Core with SQLite for local development. If needed, replace with SQL Server.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
});

// Register application services for dependency injection. These services abstract
// the data access logic away from the controllers and promote a cleaner architecture.
builder.Services.AddScoped<BookStoreMVC.Services.IBookService, BookStoreMVC.Services.BookService>();
builder.Services.AddScoped<BookStoreMVC.Services.ICartService, BookStoreMVC.Services.CartService>();
builder.Services.AddScoped<BookStoreMVC.Services.IOrderService, BookStoreMVC.Services.OrderService>();
builder.Services.AddScoped<BookStoreMVC.Services.IPaymentService, BookStoreMVC.Services.PaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply any pending migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    DbInitializer.Seed(dbContext);
}

app.Run();