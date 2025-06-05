using KTGiuaKy.Models;
using KTGiuaKy.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KTGiuaKy.Controllers
{
    public class AccountController : Controller
    {
        private readonly ISinhVienRepository _sinhVienRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ISinhVienRepository sinhVienRepository, ILogger<AccountController> logger)
        {
            _sinhVienRepository = sinhVienRepository;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sinhVien = _sinhVienRepository.GetById(model.MaSV);
                if (sinhVien != null)
                {
                    // Lưu mã sinh viên vào session
                    HttpContext.Session.SetString("MaSV", model.MaSV);
                    TempData["Success"] = "Đăng nhập thành công!";
                    return RedirectToAction("DangKyHocPhan", "DangKy");
                }
                else
                {
                    _logger.LogWarning("Đăng nhập thất bại: Mã sinh viên {MaSV} không tồn tại", model.MaSV);
                    TempData["Error"] = "Mã sinh viên không hợp lệ.";
                }
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }
    }
}