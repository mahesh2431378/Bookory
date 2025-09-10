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
        /// <summary>
        /// Adds a book to the cart. If goToCart is true, user is redirected to the cart page; otherwise back to book details.
        /// </summary>
        public async Task<IActionResult> Add(int bookId, bool goToCart = true)
        {
            await _cartService.AddToCartAsync(CurrentUserId, bookId);
            return goToCart
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Details", "Books", new { id = bookId });
        }

        // GET: /Cart/Update/5?qty=2
        /// <summary>
        /// Updates the quantity of a cart item. Quantity is clamped between 1 and available stock.
        /// </summary>
        public async Task<IActionResult> Update(int id, int qty)
        {
            await _cartService.UpdateQuantityAsync(CurrentUserId, id, qty);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Remove/5
        /// <summary>
        /// Removes an item from the cart.
        /// </summary>
        public async Task<IActionResult> Remove(int id)
        {
            await _cartService.RemoveAsync(CurrentUserId, id);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/Checkout
        /// <summary>
        /// Displays the checkout form. Redirects to cart if there are no items.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var items = await _cartService.GetCartItemsAsync(CurrentUserId);
            // Redirect if cart is empty
            if (!items.Any())
            {
                return RedirectToAction(nameof(Index));
            }
            // Calculate summary for display
            decimal subtotal = items.Sum(i => (i.Book?.Price ?? 0M) * i.Quantity);
            // Shipping cost can be adjusted here; set to zero to avoid altering order total
            decimal shipping = 0M;
            ViewBag.ItemsCount = items.Sum(i => i.Quantity);
            ViewBag.Subtotal   = subtotal;
            ViewBag.Shipping   = shipping;
            ViewBag.Total      = subtotal + shipping;
            return View();
        }

        // POST: /Cart/Checkout
        /// <summary>
        /// Processes the checkout form and creates an order with PENDING status. Clears the cart and redirects to payment.
        /// </summary>
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
                // If something went wrong (e.g., empty cart), redirect to cart
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Cart/Payment
        /// <summary>
        /// Displays the payment form for the specified order.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            if (order == null || order.UserId != CurrentUserId)
            {
                return RedirectToAction("Index", "Books");
            }
            // Compute summary values for the payment page
            decimal subtotal = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
            decimal shipping = order.TotalAmount - subtotal;
            ViewBag.OrderId    = order.Id;
            ViewBag.ItemsCount = order.OrderItems.Sum(i => i.Quantity);
            ViewBag.Subtotal   = subtotal;
            ViewBag.Shipping   = shipping;
            ViewBag.Amount     = order.TotalAmount;
            var vm = new PaymentVm
            {
                OrderId = orderId,
                Amount = order.TotalAmount,
                Method = "UPI",
                Success = true
            };
            return View(vm);
        }

        // POST: /Cart/Payment
        /// <summary>
        /// Saves a payment record and updates the order status based on success. Redirects to success page.
        /// </summary>
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
                await _paymentService.ProcessPaymentAsync(CurrentUserId, vm.OrderId, vm.Amount, vm.Method , vm.Success);
                return RedirectToAction(nameof(Success), new { orderId = vm.OrderId, ok = vm.Success });
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Books");
            }
        }
        [HttpGet]
        public async Task<IActionResult> codsuccess(int orderId)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            if (order == null || order.UserId != CurrentUserId)
            {
                return RedirectToAction("Index", "Books");
            }

            // For COD, we process the payment with success = true
            // The PaymentService logic will set the payment status to PENDING
            await _paymentService.ProcessPaymentAsync(CurrentUserId, order.Id, order.TotalAmount, "COD", true);

            // Redirect to the success page to show the order summary
            return RedirectToAction(nameof(Success), new { orderId = order.Id, ok = true });
        }
        // GET: /Cart/Success
        /// <summary>
        /// Displays a summary of the completed order along with payment outcome.
        /// </summary>
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
        /// <summary>
        /// The payment method selected by the customer. Supported values include
        /// "UPI" and "CARD". When adding new methods, ensure the view and
        /// validation logic reflect the available options.
        /// </summary>
        public string? Method { get; set; }
        /// <summary>
        /// Optional UPI identifier when the UPI method is chosen. This value is
        /// collected purely for demonstration and is not persisted. A valid UPI
        /// ID typically follows the pattern name@provider.
        /// </summary>
        public string? UpiId { get; set; }
        /// <summary>
        /// Optional card number when the card method is chosen. This should be
        /// masked or validated on the client side. Only collected for UI
        /// demonstration; not persisted in the database.
        /// </summary>
        public string? CardNumber { get; set; }
        /// <summary>
        /// Optional expiry date for card payments in MM/YY format.
        /// </summary>
        public string? Expiry { get; set; }
        /// <summary>
        /// Optional card security code (CVV). Only collected for UI demonstration.
        /// </summary>
        public string? Cvv { get; set; }
        /// <summary>
        /// Optional name on card when paying via card. Not persisted.
        /// </summary>
        public string? CardName { get; set; }
        /// <summary>
        /// Flag used to simulate success or failure during development. In a real implementation,
        /// this would be determined by the payment gateway response.
        /// </summary>
        public bool Success { get; set; } = true;
    }
}