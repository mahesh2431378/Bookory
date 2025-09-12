using BookStoreMVC.Models;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Controllers;

namespace BookStoreMVC.Services
{
    /// <summary>
    /// Implements order operations using Entity Framework.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetOrdersAsync(int userId, bool isAdmin)
        {
            IQueryable<Order> orders = _context.Orders;

            if (!isAdmin)
            {
                orders = orders.Where(o => o.UserId == userId);
            }

            orders = orders
              .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
              .Include(o => o.User)
              .Include(o => o.Payment);

            return await orders.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<Order?> GetOrderAsync(int orderId)
        {
            return await _context.Orders
              .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Book)
                  .ThenInclude(b => b.Category)
              .Include(o => o.User)
              .Include(o => o.Payment)
              .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order> CreateOrderAsync(int userId, CheckoutVm vm)
        {
            // Begin a database transaction to ensure all operations succeed or fail together.
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var cartItems = await _context.CartItems
                      .Include(ci => ci.Book)
                      .Where(ci => ci.UserId == userId)
                      .ToListAsync();

                    if (!cartItems.Any())
                    {
                        throw new InvalidOperationException("Cart is empty.");
                    }

                    decimal total = cartItems.Sum(i => i.Book!.Price * i.Quantity);

                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.UtcNow,
                        Status = OrderStatus.PENDING,
                        TotalAmount = total,
                        ShippingName = vm.FullName,
                        ShippingAddress = vm.Address,
                        ShippingCity = vm.City,
                        ShippingState = vm.State,
                        ShippingZip = vm.Zip,
                        Phone = vm.Phone
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Save to generate the new order.Id

                    foreach (var item in cartItems)
                    {
                        // Check for sufficient stock before proceeding
                        if (item.Book!.StockQuantity < item.Quantity)
                        {
                            // If stock is insufficient, the transaction will be rolled back.
                            throw new InvalidOperationException($"Not enough stock for book: {item.Book.Title}");
                        }

                        // Reduce the stock quantity of the book
                        item.Book.StockQuantity -= item.Quantity;

                        _context.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.Id,
                            BookId = item.BookId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Book!.Price
                        });
                    }

                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    // If all operations are successful, commit the transaction.
                    await transaction.CommitAsync();

                    return order;
                }
                catch (Exception)
                {
                    // If any operation fails, roll back all changes to the database.
                    await transaction.RollbackAsync();
                    throw; // Re-throw the exception.
                }
            }
        }

        public async Task UpdateStatusAsync(int orderId, OrderStatus status)
        {
            // Eagerly load the related Payment object
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                order.Status = status;

                // If the order is delivered, mark the payment as completed
                if (status == OrderStatus.DELIVERED)
                {
                    // The incorrect line has been removed.
                    if (order.Payment != null)
                    {
                        order.Payment.PaymentStatus = PaymentStatus.COMPLETED;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}