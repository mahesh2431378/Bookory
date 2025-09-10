using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Models;
using System.Security.Claims;

namespace BookStoreMVC.Controllers
{
    /// <summary>
    /// Handles shopping cart operations such as adding, updating, removing items and the checkout/payment flow.
    /// This controller requires the user to be authenticated.
    /// </summary>
    [Authorize]
    public class CartController : Controller
    {
        private readonly BookStoreMVC.Services.ICartService _cartService;
        private readonly BookStoreMVC.Services.IOrderService _orderService;
        private readonly BookStoreMVC.Services.IPaymentService _paymentService;

        public CartController(BookStoreMVC.Services.ICartService cartService,
                              BookStoreMVC.Services.IOrderService orderService,
                              BookStoreMVC.Services.IPaymentService paymentService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _paymentService = paymentService;
        }

        /// <summary>
        /// Retrieves the currently logged in user's numeric identifier from their claims.
        /// </summary>
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var items = await _cartService.GetCartItemsAsync(CurrentUserId);
            return View(items);
        }

        // GET: /Cart/Add/5
        public async Task<IActionResult> Add(int bookId, bool goToCart = true)
        {
            await _cartService.AddToCartAsync(CurrentUserId, bookId);
            return goToCart
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Details", "Books", new { id = bookId });
        }

        // GET: /Cart/Update/5?qty=2
        public async Task<IActionResult> Update(int id, int qty)
        {
            await _cartService.UpdateQuantityAsync(CurrentUserId, id, qty);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Remove/5
        public async Task<IActionResult> Remove(int id)
        {
            await _cartService.RemoveAsync(CurrentUserId, id);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var items = await _cartService.GetCartItemsAsync(CurrentUserId);
            if (!items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            decimal subtotal = items.Sum(i => (i.Book?.Price ?? 0M) * i.Quantity);
            decimal shipping = 0M; // Shipping cost can be adjusted here
            ViewBag.ItemsCount = items.Sum(i => i.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Total = subtotal + shipping;
            return View();
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            try
            {
                var order = await _orderService.CreateOrderAsync(CurrentUserId, vm);
                return RedirectToAction(nameof(Payment), new { orderId = order.Id });
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Cart/Payment
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            if (order == null || order.UserId != CurrentUserId)
            {
                return RedirectToAction("Index", "Books");
            }

            decimal subtotal = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
            decimal shipping = order.TotalAmount - subtotal;
            ViewBag.OrderId = order.Id;
            ViewBag.ItemsCount = order.OrderItems.Sum(i => i.Quantity);
            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Amount = order.TotalAmount;

            var vm = new PaymentVm
            {
                OrderId = orderId,
                Amount = order.TotalAmount,
                Method = "UPI", // Default payment method
                Success = true
            };
            return View(vm);
        }

        // POST: /Cart/Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(PaymentVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            try
            {
                // **IMPROVEMENT**: Use null-coalescing for safety.
                await _paymentService.ProcessPaymentAsync(CurrentUserId, vm.OrderId, vm.Amount, vm.Method ?? "UPI", vm.Success);
                return RedirectToAction(nameof(Success), new { orderId = vm.OrderId, ok = vm.Success });
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Books");
            }
        }

        // **FEATURE**: Includes the dedicated workflow for Cash on Delivery.
        // GET: /Cart/codsuccess?orderId=5
        [HttpGet]
        public async Task<IActionResult> CodSuccess(int orderId)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            if (order == null || order.UserId != CurrentUserId)
            {
                return RedirectToAction("Index", "Books");
            }

            // For COD, we process the payment with success = true.
            // The PaymentService will correctly set the payment status to PENDING.
            await _paymentService.ProcessPaymentAsync(CurrentUserId, order.Id, order.TotalAmount, "COD", true);

            // Redirect to the success page to show the order summary.
            return RedirectToAction(nameof(Success), new { orderId = order.Id, ok = true });
        }

        // GET: /Cart/Success
        [HttpGet]
        public async Task<IActionResult> Success(int orderId, bool ok = true)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            if (order == null || order.UserId != CurrentUserId)
            {
                return RedirectToAction("Index", "Books");
            }
            ViewBag.Ok = ok;
            return View(order);
        }
    }

    /// <summary>
    /// View model used for collecting shipping and contact details during checkout.
    /// </summary>
    public class CheckoutVm
    {
        public string FullName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Zip { get; set; } = "";
        public string Phone { get; set; } = "";
    }

    /// <summary>
    /// View model used for processing payment. Includes a flag to simulate success or failure.
    /// </summary>
    public class PaymentVm
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public string? UpiId { get; set; }
        public string? CardNumber { get; set; }
        public string? Expiry { get; set; }
        public string? Cvv { get; set; }
        public string? CardName { get; set; }
        public bool Success { get; set; } = true;
    }
}