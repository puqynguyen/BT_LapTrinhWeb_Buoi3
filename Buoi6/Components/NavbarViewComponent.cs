using Buoi6.Models;
using Buoi6.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Security.Claims; // Thêm namespace này
using System.Linq;

namespace Buoi6.Components
{
    public class NavbarViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NavbarViewComponent> _logger;

        public NavbarViewComponent(ICartService cartService, UserManager<ApplicationUser> userManager, ILogger<NavbarViewComponent> logger)
        {
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            _logger.LogInformation("Rendering NavbarViewComponent. IsAuthenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);

            var model = new NavbarViewModel
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                IsAdmin = User.IsInRole("Admin"),
                CartItemsCount = 0
            };

            if (model.IsAuthenticated)
            {
                // Ép kiểu User sang ClaimsPrincipal và kiểm tra Claims
                var claimsPrincipal = User as ClaimsPrincipal;
                if (claimsPrincipal != null)
                {
                    _logger.LogInformation("User roles: {Roles}",
                        string.Join(", ", claimsPrincipal.Claims
                            .Where(c => c.Type == ClaimTypes.Role || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                            .Select(c => c.Value)));
                }
                else
                {
                    _logger.LogWarning("User is not a ClaimsPrincipal");
                }
            }

            if (model.IsAuthenticated && !model.IsAdmin)
            {
                var userId = _userManager.GetUserId((ClaimsPrincipal)User);
                if (!string.IsNullOrEmpty(userId))
                {
                    model.CartItemsCount = await _cartService.GetCartItemsCountAsync(userId);
                    _logger.LogInformation("Cart items count for user {UserId}: {Count}", userId, model.CartItemsCount);
                }
                else
                {
                    _logger.LogWarning("UserId is null for authenticated user");
                }
            }

            return View(model);
        }
    }

    public class NavbarViewModel
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
        public int CartItemsCount { get; set; }
    }
}