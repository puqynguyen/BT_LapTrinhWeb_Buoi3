using Buoi6.Models;
using Buoi6.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Buoi6.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public ProductController(ICategoryRepository categoryRepository, IProductRepository productRepository, IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger, ApplicationDbContext dbContext)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Rendering Create view for Product");
            var categories = await _categoryRepository.GetAllAsync();
            if (categories == null || !categories.Any())
            {
                _logger.LogWarning("No categories found for product creation");
                TempData["Error"] = "Không có danh mục nào được tìm thấy. Vui lòng tạo danh mục trước.";
            }
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel productVM)
        {
            _logger.LogInformation("Attempting to create product: {Name}", productVM?.Name);
            if (productVM == null)
            {
                _logger.LogWarning("ProductViewModel is null");
                TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ";
                return await ReloadCreateView(new ProductViewModel());
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = new Product
                    {
                        Name = productVM.Name?.Trim() ?? string.Empty,
                        Price = productVM.Price,
                        Description = productVM.Description?.Trim() ?? string.Empty,
                        CategoryId = productVM.CategoryId,
                        Images = new List<ProductImage>()
                    };

                    // Save main image
                    if (productVM.MainImage != null && productVM.MainImage.Length > 0)
                    {
                        var mainImagePath = await SaveImageAsync(productVM.MainImage);
                        if (!string.IsNullOrEmpty(mainImagePath))
                        {
                            product.ImageUrl = mainImagePath;
                        }
                        else
                        {
                            ModelState.AddModelError("MainImage", "Không thể lưu hình ảnh chính.");
                        }
                    }

                    // Save product to get Id
                    await _productRepository.AddAsync(product);
                    _logger.LogInformation("Product created with ID: {Id}", product.Id);

                    // Save additional images
                    if (productVM.AdditionalImages != null && productVM.AdditionalImages.Any())
                    {
                        await ProcessAdditionalImagesAsync(productVM.AdditionalImages, product.Id);
                    }

                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product: {Name}", productVM.Name);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("ModelState invalid for product: {Name}", productVM.Name);
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Key}, Error: {Message}", state.Key, error.ErrorMessage);
                    }
                }
            }

            return await ReloadCreateView(productVM);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var images = await _dbContext.ProductImages.Where(pi => pi.ProductId == id).ToListAsync();
            var productVM = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                ExistingImages = images
            };
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductViewModel productVM, int[] deleteImages)
        {
            if (id != productVM.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _productRepository.GetProductByIdAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    string oldImageUrl = product.ImageUrl;
                    product.Name = productVM.Name?.Trim() ?? string.Empty;
                    product.Price = productVM.Price;
                    product.Description = productVM.Description?.Trim() ?? string.Empty;
                    product.CategoryId = productVM.CategoryId;

                    // Update main image
                    if (productVM.MainImage != null && productVM.MainImage.Length > 0)
                    {
                        var newImagePath = await SaveImageAsync(productVM.MainImage);
                        if (!string.IsNullOrEmpty(newImagePath))
                        {
                            product.ImageUrl = newImagePath;
                            if (!string.IsNullOrEmpty(oldImageUrl))
                            {
                                DeleteImageFile(oldImageUrl);
                            }
                        }
                    }

                    // Delete selected images
                    if (deleteImages != null && deleteImages.Any())
                    {
                        var imagesToDelete = await _dbContext.ProductImages
                            .Where(pi => deleteImages.Contains(pi.Id) && pi.ProductId == id)
                            .ToListAsync();
                        foreach (var image in imagesToDelete)
                        {
                            DeleteImageFile(image.Url);
                            _dbContext.ProductImages.Remove(image);
                        }
                    }

                    // Add new additional images
                    if (productVM.AdditionalImages != null && productVM.AdditionalImages.Any())
                    {
                        await ProcessAdditionalImagesAsync(productVM.AdditionalImages, id);
                    }

                    await _productRepository.UpdateAsync(product);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Product updated successfully: {Name}", product.Name);
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Name}", productVM.Name);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("ModelState invalid for product: {Name}", productVM.Name);
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Key}, Error: {Message}", state.Key, error.ErrorMessage);
                    }
                }
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", productVM.CategoryId);
            productVM.ExistingImages = await _dbContext.ProductImages.Where(pi => pi.ProductId == id).ToListAsync();
            return View(productVM);
        }

        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Loading details for product ID: {Id}", id);
            var product = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {Id}", id);
                return NotFound();
            }
            _logger.LogInformation("Product {Id} has {ImageCount} additional images", id, product.Images?.Count ?? 0);
            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Loading delete confirmation for product ID: {Id}", id);
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product not found: {Id}", id);
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Attempting to delete product ID: {Id}", id);
            try
            {
                var product = await _productRepository.GetProductByIdAsync(id);
                if (product != null)
                {
                    // Delete main image
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        DeleteImageFile(product.ImageUrl);
                        _logger.LogInformation("Deleted main image: {ImagePath}", product.ImageUrl);
                    }

                    // Delete additional images
                    var images = await _dbContext.ProductImages.Where(pi => pi.ProductId == id).ToListAsync();
                    foreach (var image in images)
                    {
                        DeleteImageFile(image.Url);
                        _dbContext.ProductImages.Remove(image);
                        _logger.LogInformation("Deleted additional image: {ImagePath}", image.Url);
                    }

                    // Delete product
                    await _productRepository.DeleteAsync(id);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Product deleted successfully: {Id}", id);
                    TempData["Success"] = "Xóa sản phẩm thành công!";
                }
                else
                {
                    _logger.LogWarning("Product not found for deletion: {Id}", id);
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            try
            {
                var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }
                var fileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                var savePath = Path.Combine(imagesPath, fileName);
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                return "/images/" + fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image: {FileName}", image.FileName);
                return null;
            }
        }

        private async Task ProcessAdditionalImagesAsync(IList<IFormFile> additionalImages, int productId)
        {
            if (additionalImages == null || !additionalImages.Any())
            {
                _logger.LogInformation("No additional images to process for product {ProductId}", productId);
                return;
            }

            foreach (var image in additionalImages)
            {
                if (image != null && image.Length > 0)
                {
                    var imagePath = await SaveImageAsync(image);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            Url = imagePath
                        };
                        _dbContext.ProductImages.Add(productImage);
                        _logger.LogInformation("Added additional image for product {ProductId}: {ImagePath}", productId, imagePath);
                    }
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        private void DeleteImageFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return;

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    _logger.LogInformation("Deleted image file: {Path}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image file: {Path}", imagePath);
            }
        }

        private async Task<IActionResult> ReloadCreateView(ProductViewModel productVM)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", productVM.CategoryId);
            return View(productVM);
        }

        private bool IsImageFile(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }
}