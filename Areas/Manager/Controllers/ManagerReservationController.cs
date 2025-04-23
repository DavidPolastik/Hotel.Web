using Hotel.Application.Abstraction;
using Hotel.Domain.Entities;
using Hotel.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class ManagerReservationController : Controller
    {
        private readonly IReservationService _reservationService;

        public ManagerReservationController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // Zobrazení seznamu všech rezervací
        public async Task<IActionResult> Index()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return View(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Načíst rezervaci z databáze
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            // Načíst dostupné pokoje
            var rooms = (await _reservationService.GetAvailableRoomsAsync(DateTime.MinValue, DateTime.MaxValue)).ToList();

            // Přidat aktuální pokoj rezervace do seznamu, pokud tam není
            if (!rooms.Any(r => r.Id == reservation.RoomId))
            {
                var currentRoom = await _reservationService.GetRoomByIdAsync(reservation.RoomId);
                if (currentRoom != null)
                {
                    rooms.Add(currentRoom);
                }
            }

            ViewBag.Rooms = new SelectList(rooms, "Id", "RoomNumber", reservation.RoomId);

            // Naplnit ViewModel
            var model = new ReservationEditViewModel
            {
                Id = reservation.Id,
                RoomId = reservation.RoomId,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReservationEditViewModel model)
        {
            // Validace modelu
            if (!ModelState.IsValid)
            {
                var rooms = await _reservationService.GetAvailableRoomsAsync(DateTime.MinValue, DateTime.MaxValue);
                ViewBag.Rooms = new SelectList(rooms, "Id", "RoomNumber");
                return View(model);
            }

            // Načíst existující rezervaci z databáze
            var existingReservation = await _reservationService.GetReservationByIdAsync(id);
            if (existingReservation == null)
            {
                return NotFound();
            }

            // Aktualizovat data rezervace
            existingReservation.RoomId = model.RoomId;
            existingReservation.CheckInDate = model.CheckInDate;
            existingReservation.CheckOutDate = model.CheckOutDate;

            await _reservationService.UpdateReservationAsync(existingReservation);

            TempData["SuccessMessage"] = "Reservation updated successfully.";
            return RedirectToAction(nameof(Index));
        }






        // Mazání rezervace
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            await _reservationService.DeleteReservationAsync(id);
            TempData["SuccessMessage"] = "Reservation deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
