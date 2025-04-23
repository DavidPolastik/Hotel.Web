using Hotel.Infrastructure.Data;
using Hotel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Hotel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // Zobrazení seznamu uživatelů a jejich rolí
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            ViewBag.Roles = new[] { "Admin", "Manager", "User" }; // Seznam dostupných rolí
            return View(users);
        }

        // Změna role uživatele
        [HttpPost]
        public IActionResult ChangeRole(int id, string newRole)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newRole) || !_context.Users.Any(u => newRole == "Admin" || newRole == "Manager" || newRole == "User"))
            {
                TempData["ErrorMessage"] = "Invalid role selected.";
                return RedirectToAction(nameof(Index));
            }

            user.Role = newRole;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "User role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Kontrola: Musí zůstat alespoň jeden admin
            var adminCount = _context.Users.Count(u => u.Role == "Admin");
            if (user.Role == "Admin" && adminCount <= 1)
            {
                TempData["ErrorMessage"] = "Cannot delete the last admin.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
