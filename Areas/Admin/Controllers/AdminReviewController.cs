using Hotel.Application.Abstraction;
using Hotel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hotel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public AdminReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Zobrazení seznamu recenzí
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                ModelState.AddModelError("Comment", "Comment cannot be empty.");
                var review = await _reviewService.GetReviewByIdAsync(id);
                return View(review);
            }

            try
            {
                var review = await _reviewService.GetReviewByIdAsync(id);
                if (review == null)
                {
                    return NotFound();
                }

                review.Comment = comment;
                await _reviewService.UpdateReviewAsync(review);

                TempData["SuccessMessage"] = "Review comment updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating review comment: {ex.Message}";
                var review = await _reviewService.GetReviewByIdAsync(id);
                return View(review);
            }
        }


        // Smazání recenze
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            await _reviewService.DeleteReviewAsync(id);
            TempData["SuccessMessage"] = "Review deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
