using KTGiuaKy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace KTGiuaKy.Controllers
{
    public class HocPhanViewModel
    {
        public string MaHP { get; set; }
        public string TenHP { get; set; }
        public int SoTinChi { get; set; }
        public int SoLuong { get; set; } // Số lượng sinh viên tối đa
        public int SoLuongDuKien { get; set; } // Số lượng còn lại
        public bool DaDangKy { get; set; }
    }

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

            // Lấy danh sách học phần và tính số lượng sinh viên dự kiến
            var daDangKy = _context.ChiTietDangKys
                .Where(ct => ct.DangKy.MaSV == maSV)
                .Select(ct => ct.MaHP)
                .ToList();

            var hocPhans = _context.HocPhans
                .Select(hp => new HocPhanViewModel
                {
                    MaHP = hp.MaHP,
                    TenHP = hp.TenHP,
                    SoTinChi = hp.SoTinChi,
                    SoLuong = hp.SoLuong,
                    SoLuongDuKien = hp.SoLuong - _context.ChiTietDangKys.Count(ct => ct.MaHP == hp.MaHP),
                    DaDangKy = daDangKy.Contains(hp.MaHP)
                })
                .ToList();

            return View(hocPhans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKyHocPhan(List<string> selectedHocPhans)
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

            // Lấy danh sách học phần đã đăng ký
            var daDangKy = _context.ChiTietDangKys
                .Where(ct => ct.DangKy.MaSV == maSV)
                .Select(ct => ct.MaHP)
                .ToList();

            // Lấy giỏ hàng hiện tại từ session
            var cart = HttpContext.Session.GetString("Cart") != null
                ? JsonSerializer.Deserialize<List<string>>(HttpContext.Session.GetString("Cart"))
                : new List<string>();

            // Thêm học phần mới vào giỏ hàng, tránh trùng lặp và đã đăng ký
            var addedCount = 0;
            foreach (var maHP in selectedHocPhans)
            {
                if (!cart.Contains(maHP) && !daDangKy.Contains(maHP) && _context.HocPhans.Any(hp => hp.MaHP == maHP))
                {
                    cart.Add(maHP);
                    addedCount++;
                }
            }

            if (addedCount == 0)
            {
                TempData["Error"] = "Tất cả học phần đã chọn đã được đăng ký hoặc không hợp lệ.";
                return RedirectToAction(nameof(DangKyHocPhan));
            }

            // Lưu giỏ hàng vào session
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
            TempData["Success"] = "Đã thêm học phần vào giỏ hàng!";
            return RedirectToAction(nameof(Cart));
        }

        public IActionResult Cart()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.GetString("Cart") != null
                ? JsonSerializer.Deserialize<List<string>>(HttpContext.Session.GetString("Cart"))
                : new List<string>();

            // Lấy thông tin học phần từ cơ sở dữ liệu
            var hocPhans = _context.HocPhans
                .Where(hp => cart.Contains(hp.MaHP))
                .ToList();

            return View(hocPhans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCart()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xác nhận đăng ký.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy giỏ hàng từ session
            var cart = HttpContext.Session.GetString("Cart") != null
                ? JsonSerializer.Deserialize<List<string>>(HttpContext.Session.GetString("Cart"))
                : new List<string>();

            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống. Vui lòng chọn học phần.";
                return RedirectToAction(nameof(Cart));
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

                // Thêm các học phần từ giỏ hàng vào ChiTietDangKy
                foreach (var maHP in cart)
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

                // Xóa giỏ hàng sau khi xác nhận
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = "Đăng ký học phần thành công!";
                return RedirectToAction(nameof(DaDangKy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận đăng ký học phần cho sinh viên: {MaSV}", maSV);
                TempData["Error"] = "Có lỗi xảy ra khi xác nhận đăng ký.";
                return RedirectToAction(nameof(Cart));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xóa giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            // Xóa giỏ hàng
            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Đã xóa toàn bộ giỏ hàng!";
            return RedirectToAction(nameof(Cart));
        }

        public IActionResult DaDangKy()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem học phần đã đăng ký.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách học phần đã đăng ký
            var dangKys = _context.DangKys
                .Where(dk => dk.MaSV == maSV)
                .Include(dk => dk.ChiTietDangKys)
                .ThenInclude(ct => ct.HocPhan)
                .ToList();

            // Tính tổng số học phần và tín chỉ
            var hocPhans = dangKys
                .SelectMany(dk => dk.ChiTietDangKys)
                .Select(ct => ct.HocPhan)
                .Distinct()
                .ToList();

            ViewBag.TongHocPhan = hocPhans.Count;
            ViewBag.TongTinChi = hocPhans.Sum(hp => hp.SoTinChi);

            return View(dangKys);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDangKy(int maDK, string maHP)
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xóa đăng ký.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var chiTietDangKy = await _context.ChiTietDangKys
                    .FirstOrDefaultAsync(ct => ct.MaDK == maDK && ct.MaHP == maHP && ct.DangKy.MaSV == maSV);
                if (chiTietDangKy == null)
                {
                    TempData["Error"] = "Không tìm thấy đăng ký.";
                    return RedirectToAction(nameof(DaDangKy));
                }

                _context.ChiTietDangKys.Remove(chiTietDangKy);
                await _context.SaveChangesAsync();

                // Kiểm tra nếu MaDK không còn ChiTietDangKy thì xóa DangKy
                var dangKy = await _context.DangKys
                    .Include(dk => dk.ChiTietDangKys)
                    .FirstOrDefaultAsync(dk => dk.MaDK == maDK);
                if (dangKy != null && !dangKy.ChiTietDangKys.Any())
                {
                    _context.DangKys.Remove(dangKy);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Xóa học phần đã đăng ký thành công!";
                return RedirectToAction(nameof(DaDangKy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa đăng ký học phần {MaHP} cho sinh viên {MaSV}", maHP, maSV);
                TempData["Error"] = "Có lỗi xảy ra khi xóa đăng ký.";
                return RedirectToAction(nameof(DaDangKy));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllDangKy()
        {
            // Kiểm tra đăng nhập
            var maSV = HttpContext.Session.GetString("MaSV");
            if (string.IsNullOrEmpty(maSV))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xóa tất cả đăng ký.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var dangKys = await _context.DangKys
                    .Where(dk => dk.MaSV == maSV)
                    .Include(dk => dk.ChiTietDangKys)
                    .ToListAsync();

                foreach (var dangKy in dangKys)
                {
                    _context.ChiTietDangKys.RemoveRange(dangKy.ChiTietDangKys);
                    _context.DangKys.Remove(dangKy);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa tất cả học phần đã đăng ký thành công!";
                return RedirectToAction(nameof(DaDangKy));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa tất cả đăng ký cho sinh viên {MaSV}", maSV);
                TempData["Error"] = "Có lỗi xảy ra khi xóa tất cả đăng ký.";
                return RedirectToAction(nameof(DaDangKy));
            }
        }
    }
}