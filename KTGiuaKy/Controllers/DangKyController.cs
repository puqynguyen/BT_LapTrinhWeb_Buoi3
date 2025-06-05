using KTGiuaKy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace KTGiuaKy.Controllers
{
    public class DangKyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DangKyController> _logger;

        public DangKyController(ApplicationDbContext context, ILogger<DangKyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult DangKyHocPhan()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đăng ký học phần.";
                return RedirectToAction("Login", "Account");
            }

            var hocPhans = _context.HocPhans.ToList();
            return View(hocPhans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKyHocPhan(List<string> selectedHocPhans)
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đăng ký học phần.";
                return RedirectToAction("Login", "Account");
            }

            if (selectedHocPhans == null || !selectedHocPhans.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một học phần.";
                return RedirectToAction(nameof(DangKyHocPhan));
            }

            try
            {
                // Tạo bản ghi đăng ký mới
                var dangKy = new DangKy
                {
                    MaSV = maSV,
                    NgayDK = DateTime.Now
                };
                _context.DangKys.Add(dangKy);
                await _context.SaveChangesAsync();

                // Thêm các học phần đã chọn vào ChiTietDangKy
                foreach (var maHP in selectedHocPhans)
                {
                    var hocPhan = await _context.HocPhans.FindAsync(maHP);
                    if (hocPhan != null)
                    {
                        var chiTietDangKy = new ChiTietDangKy
                        {
                            MaDK = dangKy.MaDK,
                            MaHP = maHP
                        };
                        _context.ChiTietDangKys.Add(chiTietDangKy);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đăng ký học phần thành công!";
                return RedirectToAction(nameof(DangKyHocPhan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký học phần cho sinh viên: {MaSV}", maSV);
                TempData["Error"] = "Có lỗi xảy ra khi đăng ký học phần.";
                return RedirectToAction(nameof(DangKyHocPhan));
            }
        }
    }
}