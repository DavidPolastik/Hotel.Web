using Hotel.Application.Abstraction;
using Hotel.Domain.Entities;
using Hotel.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Hotel.Web.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return View(rooms);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var imagePaths = new List<string>();

            if (model.Images != null && model.Images.Any())
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                foreach (var file in model.Images)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var fullPath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    imagePaths.Add("/uploads/" + fileName);
                }
            }

            var room = new Room
            {
                RoomName = model.RoomName,
                RoomNumber = model.RoomNumber,
                Capacity = model.Capacity,
                PricePerNight = model.PricePerNight,
                IsAvailable = model.IsAvailable,
                Images = string.Join(",", imagePaths)
            };

            await _roomService.AddRoomAsync(room);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null) return NotFound();

            var model = new EditRoomViewModel
            {
                Id = room.Id,
                RoomName = room.RoomName,
                RoomNumber = room.RoomNumber,
                Capacity = room.Capacity,
                PricePerNight = room.PricePerNight,
                IsAvailable = room.IsAvailable,
                ExistingImages = room.Images
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingRoom = await _roomService.GetRoomByIdAsync(model.Id);
            if (existingRoom == null)
            {
                return NotFound();
            }

            // Aktualizujeme základní informace
            existingRoom.RoomName = model.RoomName;
            existingRoom.RoomNumber = model.RoomNumber;
            existingRoom.Capacity = model.Capacity;
            existingRoom.PricePerNight = model.PricePerNight;
            existingRoom.IsAvailable = model.IsAvailable;

            // Zpracování existujících obrázků
            var currentImages = existingRoom.Images?.Split(',').ToList() ?? new List<string>();
            var updatedImages = model.ExistingImages?.Split(',').ToList() ?? new List<string>();

            // Najdeme obrázky, které byly odstraněny
            var removedImages = currentImages.Except(updatedImages).ToList();
            foreach (var image in removedImages)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath); // Fyzicky smažeme soubor
                }
            }

            // Aktualizujeme seznam obrázků
            existingRoom.Images = string.Join(",", updatedImages);

            // Přidání nových obrázků
            if (model.NewImages != null && model.NewImages.Any())
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                foreach (var image in model.NewImages)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    var fullPath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    updatedImages.Add("/uploads/" + fileName);
                }
            }

            existingRoom.Images = string.Join(",", updatedImages);

            // Uložíme do databáze
            await _roomService.UpdateRoomAsync(existingRoom);

            TempData["SuccessMessage"] = "Room updated successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Smažeme obrázky spojené s místností
            if (!string.IsNullOrEmpty(room.Images))
            {
                var imagePaths = room.Images.Split(',');
                foreach (var imagePath in imagePaths)
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }

            // Smažeme místnost
            await _roomService.DeleteRoomAsync(id);

            TempData["SuccessMessage"] = "Room deleted successfully.";
            return RedirectToAction(nameof(Index));
        }


    }
}
