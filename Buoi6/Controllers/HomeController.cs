using Buoi6.Models;
using Buoi6.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Buoi6.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(IProductRepository productRepository, ICategoryRepository categoryRepository, ILogger<HomeController> logger, ApplicationDbContext dbContext)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            _logger.LogInformation("Loading Home/Index with categoryId: {CategoryId}", categoryId);

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

        public IActionResult Privacy()
        {
            return View();
        }
    }
}