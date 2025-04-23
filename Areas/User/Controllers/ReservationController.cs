using Hotel.Application.Abstraction;
using Hotel.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel.Web.Areas.User.Controllers
{
    [Area("User")] // Přidání oblasti
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly ICartService _cartService;
        private readonly IReviewService _reviewService;

        public ReservationController(IReservationService reservationService, ICartService cartService, IReviewService reviewService)
        {
            _reservationService = reservationService;
            _cartService = cartService;
            _reviewService = reviewService;
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

        public async Task<IActionResult> MyReservations()
        {
            var userId = GetUserId();
            var reservations = await _reservationService.GetReservationsByUserAsync(userId);


            foreach (var reservation in reservations)
            {
                reservation.HasReview = await _reviewService.HasReviewAsync(reservation.Id);
            }

            return View(reservations);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmReservation()
        {
            try
            {
                var userId = GetUserId();
                await _reservationService.ConfirmReservationsAsync(userId);
                TempData["SuccessMessage"] = "Your reservation has been confirmed!";
                return RedirectToAction("MyReservations");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while confirming your reservation: " + ex.Message;
                return RedirectToAction("Index", "Cart", new { area = "User" });
            }
        }
    }
}
