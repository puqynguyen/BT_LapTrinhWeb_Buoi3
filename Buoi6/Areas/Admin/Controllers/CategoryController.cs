using Buoi6.Models;
using Buoi6.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Buoi6.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            ApplicationDbContext dbContext,
            ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            _logger.LogInformation("Rendering Create view for Category");
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            _logger.LogInformation("Attempting to create category: {Name}", category.Name);
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryRepository.AddAsync(category);
                    _logger.LogInformation("Category created: {Name}", category.Name);
                    TempData["Success"] = $"Đã tạo danh mục {category.Name} thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category: {Name}", category.Name);
                    ModelState.AddModelError("", "Lỗi khi thêm danh mục: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("ModelState invalid for category: {Name}", category.Name);
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Key}, Error: {Message}", state.Key, error.ErrorMessage);
                    }
                }
            }
            return View(category);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {Id}", id);
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name")] Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryRepository.UpdateAsync(category);
                    _logger.LogInformation("Category updated: {Name}", category.Name);
                    TempData["Success"] = $"Đã cập nhật danh mục {category.Name} thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category: {Name}", category.Name);
                    ModelState.AddModelError("", "Lỗi khi cập nhật danh mục: " + ex.Message);
                }
            }
            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {Id}", id);
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Attempting to delete category: {Id}", id);
            try
            {
                // Lấy danh mục để lấy tên
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found: {Id}", id);
                    TempData["Error"] = "Danh mục không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy danh sách sản phẩm thuộc danh mục
                var products = await _productRepository.GetByCategoryAsync(id);
                if (products.Any())
                {
                    // Kiểm tra các sản phẩm đã được đặt hàng
                    var productIds = products.Select(p => p.Id).ToList();
                    var orderedProducts = await _dbContext.OrderDetails
                        .Include(od => od.Product)
                        .Where(od => productIds.Contains(od.ProductId))
                        .Select(od => od.Product)
                        .Distinct()
                        .ToListAsync();

                    if (orderedProducts.Any())
                    {
                        var productNames = string.Join(", ", orderedProducts.Select(p => p.Name));
                        _logger.LogWarning("Cannot delete category {Id} ({Name}) because products [{ProductNames}] are linked to orders", id, category.Name, productNames);
                        TempData["Error"] = $"Không thể xóa danh mục '{category.Name}' vì các sản phẩm [{productNames}] đã được đặt hàng.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Xóa các sản phẩm (nếu không có đơn hàng liên quan)
                    foreach (var product in products)
                    {
                        _logger.LogInformation("Deleting product: {ProductId} for category: {CategoryId}", product.Id, id);
                        await _productRepository.DeleteAsync(product.Id);
                    }
                }

                // Xóa danh mục
                await _categoryRepository.DeleteAsync(id);
                _logger.LogInformation("Category deleted: {Id} ({Name})", id, category.Name);
                TempData["Success"] = $"Đã xóa danh mục '{category.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {Id}", id);
                TempData["Error"] = $"Lỗi khi xóa danh mục: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}