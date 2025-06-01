using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Buoi6.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Buoi6.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
            [Display(Name = "Tên đăng nhập")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Attempting login for user: {UserName}", Input.UserName);
                var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Login successful for user: {UserName}", Input.UserName);
                    var user = await _userManager.FindByNameAsync(Input.UserName);
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        _logger.LogInformation("Redirecting Admin {UserName} to /Admin/Product/Index", Input.UserName);
                        return RedirectToAction("Index", "Product", new { area = "Admin" });
                    }
                    return RedirectToAction("Index", "Home");
                }
                _logger.LogWarning("Login failed for user: {UserName}", Input.UserName);
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            return Page();
        }
    }
}