using Buoi6.Models;
using Buoi6.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Buoi6.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryRepository categoryRepository, IProductRepository productRepository, ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
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
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category: {Name}", category.Name);
                    ModelState.AddModelError("", "Lỗi khi thêm chủ đề: " + ex.Message);
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
                await _categoryRepository.UpdateAsync(category);
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var products = await _productRepository.GetByCategoryAsync(id);
            foreach (var product in products)
            {
                await _productRepository.DeleteAsync(product.Id);
            }
            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}