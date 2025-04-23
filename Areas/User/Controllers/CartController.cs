using Hotel.Application.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace Hotel.Web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("User ID claim is missing or invalid.");
            }
            return userId;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var userId = GetUserId();

            if (checkInDate >= checkOutDate)
            {
                TempData["ErrorMessage"] = "Check-out date must be later than check-in date.";
                return RedirectToAction("Index", "Room", new { area = "User" });
            }

            await _cartService.AddToCartAsync(userId, roomId, checkInDate, checkOutDate);

            TempData["SuccessMessage"] = "Room added to cart.";
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            await _cartService.RemoveFromCartAsync(cartItemId);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            await _cartService.ClearCartAsync(userId);
            return RedirectToAction("Index");
        }
    }
}
