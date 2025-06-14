using Buoi6.Models;
using Buoi6.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Buoi6.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            IOrderService orderService,
            UserManager<ApplicationUser> userManager,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _orderService = orderService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var cart = await _cartService.GetCartAsync(userId);
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["Error"] = "Có lỗi xảy ra khi tải giỏ hàng.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _cartService.AddToCartAsync(userId, productId, quantity);
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";

                var itemCount = await _cartService.GetCartItemsCountAsync(userId);
                return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", itemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                await _cartService.UpdateCartItemAsync(cartItemId, quantity);
                TempData["Success"] = "Đã cập nhật số lượng!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                await _cartService.RemoveFromCartAsync(cartItemId);
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                await _cartService.ClearCartAsync(userId);
                TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Có lỗi xảy ra khi xóa giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var cart = await _cartService.GetCartAsync(userId);

                if (!cart.Items.Any())
                {
                    TempData["Warning"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _userManager.GetUserAsync(User);
                var checkoutModel = new CheckoutViewModel
                {
                    ShippingAddress = user.Address,
                    TotalAmount = cart.TotalAmount,
                    Items = cart.Items
                };

                return View(checkoutModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang thanh toán.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var cart = await _cartService.GetCartAsync(userId);
                model.Items = cart.Items;
                model.TotalAmount = cart.TotalAmount;
                return View(model);
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                var orderId = await _orderService.CreateOrderFromCartAsync(userId, model.ShippingAddress);
                TempData["Success"] = "Đơn hàng của bạn đã được đặt thành công!";
                return RedirectToAction("OrderConfirmation", new { orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.");
                var userId = _userManager.GetUserId(User);
                var cart = await _cartService.GetCartAsync(userId);
                model.Items = cart.Items;
                model.TotalAmount = cart.TotalAmount;
                return View(model);
            }
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null || order.UserId != _userManager.GetUserId(User))
                {
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi tải thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders");
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }

        // API endpoint to get cart items count
        [HttpGet]
        public async Task<IActionResult> GetCartItemsCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var count = await _cartService.GetCartItemsCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items count");
                return Json(new { count = 0 });
            }
        }
    }
}