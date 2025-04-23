using Hotel.Application.Abstraction;
using Hotel.Application.Services;
using Hotel.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Hotel.Web.Areas.User.Controllers
{
    [Area("User")]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IReservationService _reservationService;

        public RoomController(IRoomService roomService, IReservationService reservationService)
        {
            _roomService = roomService;
            _reservationService = reservationService;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAvailableRoomsWithReviewsAsync(DateTime.Now, DateTime.Now.AddDays(1));

            foreach (var room in rooms)
            {
                Console.WriteLine($"Room {room.RoomNumber} has {room.Reviews.Count} reviews.");
                foreach (var review in room.Reviews)
                {
                    Console.WriteLine($"Review: {review.Comment}, Rating: {review.Rating}");
                }
            }

            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnavailableDates(int roomId)
        {
            var reservations = await _reservationService.GetReservationsByRoomIdAsync(roomId);

            // Vrátíme seznam obsazených dat
            var unavailableDates = reservations
                .SelectMany(res => Enumerable.Range(0, (res.CheckOutDate - res.CheckInDate).Days + 1)
                    .Select(offset => res.CheckInDate.AddDays(offset)))
                .ToList();

            return Json(unavailableDates);
        }


        [HttpPost]
        public async Task<IActionResult> BookRoom(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            try
            {
                if (checkInDate >= checkOutDate)
                {
                    TempData["ErrorMessage"] = "Datum odjezdu musí být pozdější než datum příjezdu.";
                    return RedirectToAction("Index");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Kontrola dostupnosti pokoje
                var isAvailable = await _roomService.IsRoomAvailableAsync(roomId, checkInDate, checkOutDate);
                if (!isAvailable)
                {
                    TempData["ErrorMessage"] = "Pokoj není dostupný ve zvoleném termínu.";
                    return RedirectToAction("Index");
                }

                // Vytvoření rezervace
                var reservation = new Reservation
                {
                    RoomId = roomId,
                    UserId = int.Parse(userId),
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    HasReview = false
                };

                await _reservationService.AddReservationAsync(reservation);

                TempData["SuccessMessage"] = "Pokoj byl úspěšně rezervován!";
                return RedirectToAction("MyReservations", "Reservation", new { area = "User" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Došlo k chybě: " + ex.Message;
                return RedirectToAction("Index");
            }



        }

    }
}

