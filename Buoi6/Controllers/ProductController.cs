using Buoi6.Models;
using Buoi6.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Buoi6.Controllers
{
    public class ProductController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ICategoryRepository categoryRepository, IProductRepository productRepository, IWebHostEnvironment webHostEnvironment)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var products = _productRepository.GetAllAsync().Result;
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        private async Task<string> SaveImage(IFormFile image)
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
                Console.WriteLine($"Lỗi khi lưu file: {ex.Message}");
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            // Debug: In tất cả thông tin request
            Console.WriteLine("=== DEBUG CREATE PRODUCT ===");
            Console.WriteLine($"Product Name: {product?.Name}");
            Console.WriteLine($"Product Price: {product?.Price}");
            Console.WriteLine($"Product Description: {product?.Description}");
            Console.WriteLine($"Product CategoryId: {product?.CategoryId}");

            Console.WriteLine($"ImageFile is null: {imageFile == null}");
            if (imageFile != null)
            {
                Console.WriteLine($"ImageFile Name: {imageFile.FileName}");
                Console.WriteLine($"ImageFile Size: {imageFile.Length}");
                Console.WriteLine($"ImageFile ContentType: {imageFile.ContentType}");
            }

            // In tất cả form data
            Console.WriteLine("All Form Data:");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"  {key}: {Request.Form[key]}");
            }

            // In tất cả files
            Console.WriteLine("All Files:");
            foreach (var file in Request.Form.Files)
            {
                Console.WriteLine($"  Name: {file.Name}, FileName: {file.FileName}, Size: {file.Length}");
            }

            // Loại bỏ validation cho các field không cần
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Images");
            ModelState.Remove("Category");

            // Validate file upload riêng
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn hình ảnh");
                Console.WriteLine("ERROR: No image file provided");
            }
            else if (imageFile.Length > 5 * 1024 * 1024) // 5MB
            {
                ModelState.AddModelError("imageFile", "File quá lớn. Vui lòng chọn file nhỏ hơn 5MB");
            }
            else if (!IsImageFile(imageFile))
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn file hình ảnh hợp lệ (jpg, jpeg, png, gif)");
            }

            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState Errors:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Saving image...");
                    var savedImagePath = await SaveImage(imageFile);

                    if (savedImagePath != null)
                    {
                        product.ImageUrl = savedImagePath;
                        Console.WriteLine($"Image saved to: {savedImagePath}");

                        Console.WriteLine("Adding product to database...");
                        await _productRepository.AddAsync(product);
                        Console.WriteLine("Product added successfully!");

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        Console.WriteLine("Failed to save image");
                        ModelState.AddModelError("", "Không thể lưu hình ảnh. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in Create: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
                }
            }

            // Reload categories cho view
            Console.WriteLine("Reloading view with errors...");
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product?.CategoryId);
            return View(product);
        }

        private bool IsImageFile(IFormFile file)
        {
            if (file == null) return false;

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        // Các method khác giữ nguyên...
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile imageFile)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Images");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var savedImagePath = await SaveImage(imageFile);
                        if (savedImagePath != null)
                        {
                            product.ImageUrl = savedImagePath;
                        }
                    }

                    await _productRepository.UpdateAsync(product);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating product: {ex.Message}");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm.");
                }
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}