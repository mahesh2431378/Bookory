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
            await _context.SaveChangesAsync(); // Save to get the new order.Id

            foreach (var item in cartItems)
            {
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

            return order;
        }

        public async Task UpdateStatusAsync(int orderId, OrderStatus status)
        {
            // Eagerly load the related Payment object to ensure it can be updated
            var order = await _context.Orders
                                    .Include(o => o.Payment)
                                    .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                order.Status = status;

                // If the order is delivered, mark the payment as completed
                if (status == OrderStatus.DELIVERED)
                {
                    order.PaymentStatus = PaymentStatus.COMPLETED;
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