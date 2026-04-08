using Hangfire;
using MainSpringTwo.Web.Jobs;
using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Models.Plugins;
using MainSpringTwo.Web.Models.ViewModels;
using MainSpringTwo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Controllers
{
    public class ScheduledJobController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PluginRegistry _pluginRegistry;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ScheduledJobController(AppDbContext db, PluginRegistry pluginRegistry, IBackgroundJobClient backgroundJobClient)
        {
            _db = db;
            _pluginRegistry = pluginRegistry;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _db.ScheduledJobs
                .AsNoTracking()
                .Include(job => job.Plugin)
                .Include(job => job.ConfigurationValues)
                .OrderBy(job => job.Name)
                .ToListAsync();

            ViewData["Section"] = "Job orchestration";
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var scheduledJob = await _db.ScheduledJobs
                .AsNoTracking()
                .Include(job => job.Plugin)
                .Include(job => job.ConfigurationValues)
                .FirstOrDefaultAsync(job => job.ScheduledJobId == id);

            if (scheduledJob is null)
            {
                return NotFound();
            }

            ViewData["Section"] = "Job orchestration";
            return View(scheduledJob);
        }

        public async Task<IActionResult> Create()
        {
            var model = await BuildViewModelAsync();
            ViewData["Section"] = "Job orchestration";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduledJobViewModel model)
        {
            var plugin = await ValidateScheduledJobAsync(model);

            if (!ModelState.IsValid || plugin is null)
            {
                ViewData["Toast"] = "Please review the validation errors before saving the scheduled job.";
                ViewData["ToastType"] = "error";
                await PopulateViewModelAsync(model);
                ViewData["Section"] = "Job orchestration";
                return View(model);
            }

            var now = DateTime.UtcNow;
            model.ScheduledJob.StartTime = NormalizeToUtc(model.ScheduledJob.StartTime);
            model.ScheduledJob.InsertDate = now;
            model.ScheduledJob.UpdateDate = now;

            try
            {
                _db.ScheduledJobs.Add(model.ScheduledJob);
                await _db.SaveChangesAsync();

                await ReplaceConfigurationValuesAsync(model.ScheduledJob, plugin.ConfigurationParameters, model.ConfigurationValues, now);
                await _db.SaveChangesAsync();
                TempData["Toast"] = "Scheduled job created.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ViewData["Toast"] = "The scheduled job could not be saved. Try again.";
                ViewData["ToastType"] = "error";
                await PopulateViewModelAsync(model);
                ViewData["Section"] = "Job orchestration";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var scheduledJob = await _db.ScheduledJobs
                .Include(job => job.ConfigurationValues)
                .FirstOrDefaultAsync(job => job.ScheduledJobId == id);

            if (scheduledJob is null)
            {
                return NotFound();
            }

            var model = await BuildViewModelAsync(scheduledJob);
            ViewData["Section"] = "Job orchestration";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ScheduledJobViewModel model)
        {
            if (id != model.ScheduledJob.ScheduledJobId)
            {
                return NotFound();
            }

            var existingJob = await _db.ScheduledJobs
                .Include(job => job.ConfigurationValues)
                .FirstOrDefaultAsync(job => job.ScheduledJobId == id);

            if (existingJob is null)
            {
                return NotFound();
            }

            var plugin = await ValidateScheduledJobAsync(model);

            if (!ModelState.IsValid || plugin is null)
            {
                model.ScheduledJob.InsertDate = existingJob.InsertDate;
                ViewData["Toast"] = "Please review the validation errors before saving the scheduled job.";
                ViewData["ToastType"] = "error";
                await PopulateViewModelAsync(model);
                ViewData["Section"] = "Job orchestration";
                return View(model);
            }

            var now = DateTime.UtcNow;
            existingJob.Name = model.ScheduledJob.Name.Trim();
            existingJob.PluginId = model.ScheduledJob.PluginId;
            existingJob.ScheduleType = model.ScheduledJob.ScheduleType;
            existingJob.Interval = model.ScheduledJob.Interval;
            existingJob.StartTime = NormalizeToUtc(model.ScheduledJob.StartTime);
            existingJob.IsActive = model.ScheduledJob.IsActive;
            existingJob.UpdateDate = now;

            try
            {
                await ReplaceConfigurationValuesAsync(existingJob, plugin.ConfigurationParameters, model.ConfigurationValues, now);
                await _db.SaveChangesAsync();

                TempData["Toast"] = "Scheduled job updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                model.ScheduledJob.InsertDate = existingJob.InsertDate;
                ViewData["Toast"] = "The scheduled job could not be updated. Try again.";
                ViewData["ToastType"] = "error";
                await PopulateViewModelAsync(model);
                ViewData["Section"] = "Job orchestration";
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var scheduledJob = await _db.ScheduledJobs
                .AsNoTracking()
                .Include(job => job.Plugin)
                .Include(job => job.ConfigurationValues)
                .FirstOrDefaultAsync(job => job.ScheduledJobId == id);

            if (scheduledJob is null)
            {
                return NotFound();
            }

            ViewData["Section"] = "Job orchestration";
            return View(scheduledJob);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var scheduledJob = await _db.ScheduledJobs
                .Include(job => job.ConfigurationValues)
                .FirstOrDefaultAsync(job => job.ScheduledJobId == id);

            if (scheduledJob is null)
            {
                return RedirectToAction(nameof(Index));
            }

            _db.ConfigurationValues.RemoveRange(scheduledJob.ConfigurationValues);
            _db.ScheduledJobs.Remove(scheduledJob);
            await _db.SaveChangesAsync();

            TempData["Toast"] = "Scheduled job deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunNow(int id)
        {
            _backgroundJobClient.Enqueue<PluginExecutorJob>(job => job.RunSingleJobAsync(id));
            TempData["Toast"] = "Job queued for immediate execution.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<ScheduledJobViewModel> BuildViewModelAsync(ScheduledJob? scheduledJob = null)
        {
            var model = scheduledJob is null
                ? new ScheduledJobViewModel()
                : new ScheduledJobViewModel
                {
                    ScheduledJob = scheduledJob,
                    ConfigurationValues = scheduledJob.ConfigurationValues
                        .OrderBy(value => value.ParameterName)
                        .ToList()
                };

            await PopulateViewModelAsync(model);
            return model;
        }

        private async Task PopulateViewModelAsync(ScheduledJobViewModel model)
        {
            var plugins = await _db.Plugins
                .AsNoTracking()
                .OrderBy(plugin => plugin.Name)
                .ToListAsync();

            if (model.ScheduledJob.PluginId == 0 && plugins.Count > 0)
            {
                model.ScheduledJob.PluginId = plugins[0].PluginId;
            }

            var selectedPluginEntity = plugins.FirstOrDefault(plugin => plugin.PluginId == model.ScheduledJob.PluginId);
            var selectedPlugin = selectedPluginEntity is null
                ? null
                : _pluginRegistry.GetByName(selectedPluginEntity.Name);

            var existingValues = model.ConfigurationValues
                .GroupBy(value => value.ParameterName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            model.PluginParameters = selectedPlugin?.ConfigurationParameters ?? [];
            model.ConfigurationValues = model.PluginParameters
                .Select(parameter => existingValues.TryGetValue(parameter.Name, out var existing)
                    ? new ConfigurationValue
                    {
                        ConfigurationValueId = existing.ConfigurationValueId,
                        ScheduledJobId = existing.ScheduledJobId,
                        ParameterName = existing.ParameterName,
                        Value = existing.Value,
                        InsertDate = existing.InsertDate,
                        UpdateDate = existing.UpdateDate
                    }
                    : new ConfigurationValue
                    {
                        ParameterName = parameter.Name,
                        ScheduledJobId = model.ScheduledJob.ScheduledJobId
                    })
                .ToList();

            model.PluginOptions = new SelectList(plugins, nameof(Plugin.PluginId), nameof(Plugin.Name), model.ScheduledJob.PluginId);
            model.ScheduleTypeOptions = new SelectList(
                Enum.GetValues<ScheduleType>().Select(value => new SelectListItem
                {
                    Value = value.ToString(),
                    Text = value.ToString()
                }),
                nameof(SelectListItem.Value),
                nameof(SelectListItem.Text),
                model.ScheduledJob.ScheduleType.ToString());
        }

        private async Task<IPlugin?> ValidateScheduledJobAsync(ScheduledJobViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ScheduledJob.Name))
            {
                ModelState.AddModelError("ScheduledJob.Name", "Job name is required.");
            }

            if (model.ScheduledJob.Interval <= 0)
            {
                ModelState.AddModelError("ScheduledJob.Interval", "Interval must be greater than zero.");
            }

            if (model.ScheduledJob.StartTime == default)
            {
                ModelState.AddModelError("ScheduledJob.StartTime", "Start time is required.");
            }

            var selectedPlugin = await _db.Plugins
                .AsNoTracking()
                .FirstOrDefaultAsync(plugin => plugin.PluginId == model.ScheduledJob.PluginId);

            if (selectedPlugin is null)
            {
                ModelState.AddModelError("ScheduledJob.PluginId", "A valid plugin is required.");
                return null;
            }

            var plugin = _pluginRegistry.GetByName(selectedPlugin.Name);
            if (plugin is null)
            {
                ModelState.AddModelError("ScheduledJob.PluginId", "The selected plugin is not registered.");
                return null;
            }

            model.ScheduledJob.Name = model.ScheduledJob.Name?.Trim() ?? string.Empty;
            NormalizeConfigurationValues(model.ConfigurationValues, plugin.ConfigurationParameters);
            ValidateConfigurationValues(model.ConfigurationValues, plugin.ConfigurationParameters);

            return plugin;
        }

        private void NormalizeConfigurationValues(List<ConfigurationValue> values, List<PluginParameter> parameters)
        {
            var allowed = parameters.ToDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                if (!allowed.TryGetValue(value.ParameterName, out var parameter))
                {
                    continue;
                }

                value.ParameterName = parameter.Name;
                value.Value = NormalizeConfigurationValue(parameter, value.Value);
            }
        }

        private void ValidateConfigurationValues(List<ConfigurationValue> values, List<PluginParameter> parameters)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var value = values.ElementAtOrDefault(i)?.Value ?? string.Empty;

                if (parameter.DataType == ParameterDataType.List &&
                    !string.IsNullOrWhiteSpace(value) &&
                    !parameter.Options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal)))
                {
                    ModelState.AddModelError($"ConfigurationValues[{i}].Value", $"Select a valid value for {parameter.Name}.");
                }
            }
        }

        private static string NormalizeConfigurationValue(PluginParameter parameter, string? value)
        {
            var normalizedValue = value?.Trim() ?? string.Empty;

            if (parameter.DataType == ParameterDataType.Boolean)
            {
                return string.Equals(normalizedValue, "true", StringComparison.OrdinalIgnoreCase)
                    .ToString()
                    .ToLowerInvariant();
            }

            if (parameter.DataType == ParameterDataType.List)
            {
                var selectedOption = parameter.Options.FirstOrDefault(option =>
                    string.Equals(option.Value, normalizedValue, StringComparison.OrdinalIgnoreCase));

                return selectedOption?.Value ?? normalizedValue;
            }

            return normalizedValue;
        }

        private async Task ReplaceConfigurationValuesAsync(
            ScheduledJob scheduledJob,
            List<PluginParameter> parameters,
            List<ConfigurationValue> submittedValues,
            DateTime timestamp)
        {
            var submittedLookup = submittedValues
                .Where(value => !string.IsNullOrWhiteSpace(value.ParameterName))
                .GroupBy(value => value.ParameterName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            _db.ConfigurationValues.RemoveRange(scheduledJob.ConfigurationValues);
            scheduledJob.ConfigurationValues.Clear();

            foreach (var parameter in parameters)
            {
                submittedLookup.TryGetValue(parameter.Name, out var submittedValue);
                var normalizedValue = submittedValue?.Value?.Trim() ?? string.Empty;

                var configurationValue = new ConfigurationValue
                {
                    ScheduledJobId = scheduledJob.ScheduledJobId,
                    ParameterName = parameter.Name,
                    Value = NormalizeConfigurationValue(parameter, normalizedValue),
                    InsertDate = timestamp,
                    UpdateDate = timestamp
                };

                scheduledJob.ConfigurationValues.Add(configurationValue);
            }

            await Task.CompletedTask;
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Local);
            }

            return value.ToUniversalTime();
        }
    }
}
