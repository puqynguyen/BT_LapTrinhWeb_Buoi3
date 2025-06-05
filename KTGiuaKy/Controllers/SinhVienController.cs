using KTGiuaKy.Models;
using KTGiuaKy.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Extensions.Logging;

namespace KTGiuaKy.Controllers
{
    public class SinhVienController : Controller
    {
        private readonly ISinhVienRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SinhVienController> _logger;

        public SinhVienController(ISinhVienRepository repository, ApplicationDbContext context, IWebHostEnvironment environment, ILogger<SinhVienController> logger)
        {
            _repository = repository;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var sinhViens = _repository.GetAll();
            return View(sinhViens);
        }

        public IActionResult Details(string id)
        {
            var sinhVien = _repository.GetById(id);
            if (sinhVien == null) return NotFound();
            return View(sinhVien);
        }

        public IActionResult Create()
        {
            var model = new SinhVienViewModel
            {
                MaNganhList = _context.NganhHocs
                    .Select(n => new SelectListItem { Value = n.MaNganh, Text = n.TenNganh })
                    .ToList()
            };
            if (!model.MaNganhList.Any())
            {
                _logger.LogWarning("No NganhHoc found for SinhVien creation");
                TempData["Error"] = "Không có ngành học nào được tìm thấy. Vui lòng tạo ngành học trước.";
                return View(model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SinhVienViewModel model)
        {
            _logger.LogInformation("Attempting to create SinhVien: {MaSV}, MaNganh: {MaNganh}, MainImage: {HasImage}",
                model.MaSV, model.MaNganh, model.MainImage != null);

            if (ModelState.IsValid)
            {
                try
                {
                    var sinhVien = new SinhVien
                    {
                        MaSV = model.MaSV,
                        HoTen = model.HoTen?.Trim() ?? string.Empty,
                        GioiTinh = model.GioiTinh?.Trim() ?? string.Empty,
                        NgaySinh = model.NgaySinh,
                        MaNganh = model.MaNganh
                    };

                    // Kiểm tra MaNganh
                    var nganhHoc = await _context.NganhHocs.FindAsync(model.MaNganh);
                    if (nganhHoc == null)
                    {
                        ModelState.AddModelError("MaNganh", "Ngành học không hợp lệ");
                        return await ReloadCreateView(model);
                    }

                    // Xử lý MainImage
                    if (model.MainImage != null && model.MainImage.Length > 0)
                    {
                        if (!IsImageFile(model.MainImage))
                        {
                            ModelState.AddModelError("MainImage", "Định dạng không hợp lệ: Vui lòng chọn JPG, JPEG, PNG, hoặc GIF");
                            return await ReloadCreateView(model);
                        }
                        if (model.MainImage.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("MainImage", "File quá lớn: Tối đa 5MB");
                            return await ReloadCreateView(model);
                        }

                        var imagePath = await SaveImageAsync(model.MainImage);
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            sinhVien.Hinh = imagePath;
                        }
                        else
                        {
                            ModelState.AddModelError("MainImage", "Không thể lưu hình ảnh");
                            return await ReloadCreateView(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("MainImage", "Hình ảnh là bắt buộc");
                        return await ReloadCreateView(model);
                    }

                    _repository.Add(sinhVien);
                    TempData["Success"] = "Thêm sinh viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating SinhVien: {MaSV}", model.MaSV);
                    TempData["Error"] = "Có lỗi xảy ra khi thêm sinh viên: " + ex.Message;
                }
            }
            else
            {
                _logger.LogWarning("ModelState invalid for SinhVien: {MaSV}", model.MaSV);
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Field: {Key}, Error: {Message}", state.Key, error.ErrorMessage);
                    }
                }
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
            }
            return await ReloadCreateView(model);
        }

        public IActionResult Edit(string id)
        {
            var sinhVien = _repository.GetById(id);
            if (sinhVien == null) return NotFound();
            var model = new SinhVienViewModel
            {
                MaSV = sinhVien.MaSV,
                HoTen = sinhVien.HoTen,
                GioiTinh = sinhVien.GioiTinh,
                NgaySinh = sinhVien.NgaySinh,
                Hinh = sinhVien.Hinh,
                MaNganh = sinhVien.MaNganh,
                MaNganhList = _context.NganhHocs
                    .Select(n => new SelectListItem { Value = n.MaNganh, Text = n.TenNganh })
                    .ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, SinhVienViewModel model)
        {
            if (id != model.MaSV) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var sinhVien = _repository.GetById(id);
                    if (sinhVien == null) return NotFound();

                    sinhVien.HoTen = model.HoTen?.Trim() ?? string.Empty;
                    sinhVien.GioiTinh = model.GioiTinh?.Trim() ?? string.Empty;
                    sinhVien.NgaySinh = model.NgaySinh;
                    sinhVien.MaNganh = model.MaNganh;

                    var nganhHoc = await _context.NganhHocs.FindAsync(model.MaNganh);
                    if (nganhHoc == null)
                    {
                        ModelState.AddModelError("MaNganh", "Ngành học không hợp lệ");
                        return await ReloadEditView(id, model);
                    }

                    if (model.MainImage != null && model.MainImage.Length > 0)
                    {
                        if (!IsImageFile(model.MainImage))
                        {
                            ModelState.AddModelError("MainImage", "Định dạng không hợp lệ: Vui lòng chọn JPG, JPEG, PNG, hoặc GIF");
                            return await ReloadEditView(id, model);
                        }
                        if (model.MainImage.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("MainImage", "File quá lớn: Tối đa 5MB");
                            return await ReloadEditView(id, model);
                        }

                        if (!string.IsNullOrEmpty(sinhVien.Hinh))
                        {
                            DeleteImageFile(sinhVien.Hinh);
                        }

                        var imagePath = await SaveImageAsync(model.MainImage);
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            sinhVien.Hinh = imagePath;
                        }
                        else
                        {
                            ModelState.AddModelError("MainImage", "Không thể lưu hình ảnh");
                            return await ReloadEditView(id, model);
                        }
                    }

                    _repository.Update(sinhVien);
                    TempData["Success"] = "Cập nhật sinh viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating SinhVien: {MaSV}", model.MaSV);
                    TempData["Error"] = "Có lỗi xảy ra khi cập nhật sinh viên: " + ex.Message;
                }
            }
            return await ReloadEditView(id, model);
        }

        public IActionResult Delete(string id)
        {
            var sinhVien = _repository.GetById(id);
            if (sinhVien == null) return NotFound();
            return View(sinhVien);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var sinhVien = _repository.GetById(id);
            if (sinhVien != null)
            {
                if (!string.IsNullOrEmpty(sinhVien.Hinh))
                {
                    DeleteImageFile(sinhVien.Hinh);
                }
                _repository.Delete(id);
                TempData["Success"] = "Xóa sinh viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            try
            {
                var imagesPath = Path.Combine(_environment.WebRootPath, "images");
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

        private void DeleteImageFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return;
                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image file: {Path}", imagePath);
            }
        }

        private async Task<IActionResult> ReloadCreateView(SinhVienViewModel model)
        {
            model.MaNganhList = _context.NganhHocs
                .Select(n => new SelectListItem { Value = n.MaNganh, Text = n.TenNganh })
                .ToList();
            return View(model);
        }

        private async Task<IActionResult> ReloadEditView(string id, SinhVienViewModel model)
        {
            model.MaNganhList = _context.NganhHocs
                .Select(n => new SelectListItem { Value = n.MaNganh, Text = n.TenNganh })
                .ToList();
            return View(model);
        }

        private bool IsImageFile(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }
    }
}