using Hotel.Application.Abstraction;
using Hotel.Domain.Entities;
using Hotel.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel.Web.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public IActionResult AddReview(int reservationId)
        {
            return View(new ReviewViewModel { ReservationId = reservationId });
        }


        [HttpPost]
        public async Task<IActionResult> SubmitReview(ReviewViewModel model)
        {
            if (model.Rating < 1 || model.Rating > 5)
            {
                ModelState.AddModelError("Rating", "Prosím, vyberte platné hodnocení (1-5 hvězdiček).");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vaše recenze obsahuje chyby. Zkontrolujte všechna pole.";
                return RedirectToAction("MyReservations", "Reservation", new { area = "User" });
            }

            try
            {
                var review = new Review
                {
                    UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    ReservationId = model.ReservationId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    Date = DateTime.Now
                };

                await _reviewService.AddReviewAsync(review);
                TempData["SuccessMessage"] = "Vaše recenze byla úspěšně přidána.";
                return RedirectToAction("MyReservations", "Reservation", new { area = "User" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SubmitReview: {ex.Message}");
                TempData["ErrorMessage"] = "Došlo k chybě při ukládání recenze. Zkuste to znovu.";
                return RedirectToAction("MyReservations", "Reservation", new { area = "User" });
            }
        }










    }
}





