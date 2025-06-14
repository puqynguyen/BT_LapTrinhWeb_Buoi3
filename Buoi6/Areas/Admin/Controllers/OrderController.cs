using Buoi6.Models;
using Buoi6.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Buoi6.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            ApplicationDbContext dbContext,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                _logger.LogInformation("Retrieved {Count} orders for admin", orders.Count);
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for admin");
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return View(new List<Order>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", id);
                    return NotFound();
                }
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details: {OrderId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải chi tiết đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                if (status != "Delivered")
                {
                    TempData["Error"] = "Chỉ có thể chuyển trạng thái sang 'Delivered'.";
                    return RedirectToAction(nameof(Index));
                }

                var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    TempData["Error"] = "Đơn hàng không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                if (order.Status != "Pending")
                {
                    _logger.LogWarning("Order {OrderId} is not in Pending status, current status: {Status}", orderId, order.Status);
                    TempData["Error"] = $"Đơn hàng đang ở trạng thái '{order.Status}', không thể chuyển sang 'Delivered'.";
                    return RedirectToAction(nameof(Index));
                }

                await _orderService.UpdateOrderStatusAsync(orderId, status);
                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
                TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành 'Delivered'.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}