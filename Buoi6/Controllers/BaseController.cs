using Buoi6.Models;
using Buoi6.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace Buoi6.Controllers
{
    public abstract class BaseController : Controller
    {
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        protected BaseController(ICartService cartService, UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _userManager = userManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity.IsAuthenticated && !User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    ViewBag.CartItemsCount = await _cartService.GetCartItemsCountAsync(userId);
                }
                else
                {
                    ViewBag.CartItemsCount = 0;
                }
            }
            else
            {
                ViewBag.CartItemsCount = 0;
            }

            await next();
        }
    }
}