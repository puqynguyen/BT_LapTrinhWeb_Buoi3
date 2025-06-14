using Buoi6.Models;
using Buoi6.Repository;
using Buoi6.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Buoi6.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ICartService cartService,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger,
            ApplicationDbContext dbContext)
            : base(cartService, userManager)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            _logger.LogInformation("Loading Home/Index with categoryId: {CategoryId}", categoryId);
            _logger.LogInformation("User authenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);
            _logger.LogInformation("User name: {UserName}", User.Identity.Name);
            _logger.LogInformation("User roles: {Roles}", string.Join(", ", User.Claims.Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value)));

            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                if (categories == null || !categories.Any())
                {
                    _logger.LogWarning("No categories found");
                    ViewBag.Categories = new SelectList(new List<Category>(), "Id", "Name");
                    TempData["Warning"] = "Chưa có danh mục nào được tạo.";
                }
                else
                {
                    ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
                }

                var products = categoryId.HasValue
                    ? await _productRepository.GetByCategoryAsync(categoryId.Value)
                    : await _productRepository.GetAllAsync();

                if (products == null || !products.Any())
                {
                    _logger.LogInformation("No products found for categoryId: {CategoryId}", categoryId);
                    TempData["Info"] = "Chưa có sản phẩm nào.";
                }

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Home/Index with categoryId: {CategoryId}", categoryId);
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách sản phẩm.";
                return View(new List<Product>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Loading details for product ID: {Id}", id);
            _logger.LogInformation("User authenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);
            _logger.LogInformation("User name: {UserName}", User.Identity.Name);

            try
            {
                var product = await _dbContext.Products
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found: {Id}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction(nameof(Index));
                }

                // Get cart items count for logged in users (but not for admin)
                if (User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
                {
                    try
                    {
                        var userId = _userManager.GetUserId(User);
                        if (!string.IsNullOrEmpty(userId))
                        {
                            var cartCount = await _cartService.GetCartItemsCountAsync(userId);
                            ViewBag.CartItemsCount = cartCount;
                            _logger.LogInformation("Cart items count for user {UserId}: {Count}", userId, cartCount);
                        }
                    }
                    catch (Exception cartEx)
                    {
                        _logger.LogError(cartEx, "Error getting cart count for user");
                        ViewBag.CartItemsCount = 0;
                    }
                }
                else
                {
                    ViewBag.CartItemsCount = 0;
                }

                _logger.LogInformation("Product {Id} has {ImageCount} additional images", id, product.Images?.Count ?? 0);
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for product ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải chi tiết sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.", redirectToLogin = true });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                await _cartService.AddToCartAsync(userId, productId, quantity);

                var itemCount = await _cartService.GetCartItemsCountAsync(userId);
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", itemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng." });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
    }