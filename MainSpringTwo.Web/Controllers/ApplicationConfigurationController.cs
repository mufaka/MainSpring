using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Controllers
{
    public class ApplicationConfigurationController : Controller
    {
        private readonly AppDbContext _db;

        public ApplicationConfigurationController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _db.ApplicationConfigurations
                .AsNoTracking()
                .OrderBy(config => config.CategoryName)
                .ThenBy(config => config.ConfigurationName)
                .ToListAsync();

            ViewData["Section"] = "System settings";
            return View(model);
        }

        public IActionResult Create()
        {
            ViewData["Section"] = "System settings";
            return View(new ApplicationConfiguration());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationConfiguration model)
        {
            await ValidateConfigurationAsync(model);

            if (!ModelState.IsValid)
            {
                ViewData["Toast"] = "Please review the validation errors before saving the configuration entry.";
                ViewData["ToastType"] = "error";
                ViewData["Section"] = "System settings";
                return View(model);
            }

            model.InsertDate = DateTime.UtcNow;
            model.UpdateDate = model.InsertDate;

            try
            {
                _db.ApplicationConfigurations.Add(model);
                await _db.SaveChangesAsync();

                TempData["Toast"] = "Configuration entry created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ViewData["Toast"] = "The configuration entry could not be saved. Try again.";
                ViewData["ToastType"] = "error";
                ViewData["Section"] = "System settings";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.ApplicationConfigurations.FindAsync(id);
            if (model is null)
            {
                return NotFound();
            }

            ViewData["Section"] = "System settings";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApplicationConfiguration model)
        {
            if (id != model.ApplicationConfigurationId)
            {
                return NotFound();
            }

            var existing = await _db.ApplicationConfigurations.FindAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            await ValidateConfigurationAsync(model);

            if (!ModelState.IsValid)
            {
                ViewData["Toast"] = "Please review the validation errors before saving the configuration entry.";
                ViewData["ToastType"] = "error";
                ViewData["Section"] = "System settings";
                return View(model);
            }

            existing.CategoryName = model.CategoryName.Trim();
            existing.ConfigurationName = model.ConfigurationName.Trim();
            existing.ConfigurationValue = model.ConfigurationValue?.Trim() ?? string.Empty;
            existing.UpdateDate = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();

                TempData["Toast"] = "Configuration entry updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ViewData["Toast"] = "The configuration entry could not be updated. Try again.";
                ViewData["ToastType"] = "error";
                ViewData["Section"] = "System settings";
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var model = await _db.ApplicationConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(config => config.ApplicationConfigurationId == id);

            if (model is null)
            {
                return NotFound();
            }

            ViewData["Section"] = "System settings";
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _db.ApplicationConfigurations.FindAsync(id);
            if (model is null)
            {
                return RedirectToAction(nameof(Index));
            }

            _db.ApplicationConfigurations.Remove(model);
            await _db.SaveChangesAsync();

            TempData["Toast"] = "Configuration entry deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateConfigurationAsync(ApplicationConfiguration model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(nameof(ApplicationConfiguration.CategoryName), "Category name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfigurationName))
            {
                ModelState.AddModelError(nameof(ApplicationConfiguration.ConfigurationName), "Configuration name is required.");
            }

            if (ModelState.ErrorCount > 0)
            {
                return;
            }

            var trimmedCategory = model.CategoryName.Trim();
            var trimmedName = model.ConfigurationName.Trim();

            var duplicateExists = await _db.ApplicationConfigurations.AnyAsync(config =>
                config.ApplicationConfigurationId != model.ApplicationConfigurationId &&
                config.CategoryName == trimmedCategory &&
                config.ConfigurationName == trimmedName);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(ApplicationConfiguration.ConfigurationName), "A configuration entry with this category and name already exists.");
            }
        }
    }
}
