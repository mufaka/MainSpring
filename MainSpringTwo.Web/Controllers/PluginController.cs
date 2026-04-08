using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.ViewModels;
using MainSpringTwo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Controllers
{
    public class PluginController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PluginRegistry _pluginRegistry;

        public PluginController(AppDbContext db, PluginRegistry pluginRegistry)
        {
            _db = db;
            _pluginRegistry = pluginRegistry;
        }

        public async Task<IActionResult> Index()
        {
            var registeredPlugins = _pluginRegistry.GetAll()
                .OrderBy(plugin => plugin.Name)
                .ToList();

            var persistedPlugins = await _db.Plugins
                .AsNoTracking()
                .ToDictionaryAsync(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase);

            var model = registeredPlugins
                .Select(plugin =>
                {
                    persistedPlugins.TryGetValue(plugin.Name, out var persisted);

                    return new PluginCatalogItemViewModel
                    {
                        Name = plugin.Name,
                        Description = persisted?.Description ?? plugin.Description,
                        ParameterCount = plugin.ConfigurationParameters.Count,
                        IsRegistered = true,
                        IsActive = persisted?.IsActive ?? true
                    };
                })
                .ToList();

            ViewData["Section"] = "Plugin catalog";
            return View(model);
        }

        public async Task<IActionResult> Details(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return NotFound();
            }

            var plugin = _pluginRegistry.GetByName(name);
            var persisted = await _db.Plugins
                .AsNoTracking()
                .FirstOrDefaultAsync(entry => entry.Name == name);

            if (plugin is null && persisted is null)
            {
                return NotFound();
            }

            var model = new PluginDetailsViewModel
            {
                Name = plugin?.Name ?? persisted!.Name,
                Description = persisted?.Description ?? plugin?.Description ?? string.Empty,
                IsRegistered = plugin is not null,
                IsActive = persisted?.IsActive ?? false,
                Parameters = plugin?.ConfigurationParameters ?? []
            };

            ViewData["Section"] = "Plugin catalog";
            return View(model);
        }
    }
}
