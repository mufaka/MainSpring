using MainSpringTwo.Web.Models;
using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.ViewModels;
using MainSpringTwo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MainSpringTwo.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly PluginRegistry _pluginRegistry;

        public HomeController(AppDbContext db, PluginRegistry pluginRegistry)
        {
            _db = db;
            _pluginRegistry = pluginRegistry;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var recentWindow = now.AddHours(-24);

            var activeJobs = await _db.ScheduledJobs
                .AsNoTracking()
                .Where(job => job.IsActive)
                .Include(job => job.Plugin)
                .OrderBy(job => job.Name)
                .ToListAsync();

            var recentEntries = await _db.JobHistories
                .AsNoTracking()
                .Include(entry => entry.ScheduledJob)
                .OrderByDescending(entry => entry.RunTime)
                .Take(10)
                .ToListAsync();

            var recentSuccessCount = await _db.JobHistories
                .AsNoTracking()
                .CountAsync(entry => entry.RunTime >= recentWindow && !entry.IsError);

            var recentErrorCount = await _db.JobHistories
                .AsNoTracking()
                .CountAsync(entry => entry.RunTime >= recentWindow && entry.IsError);

            var nextUpcoming = activeJobs
                .Select(job => new
                {
                    Job = job,
                    RunTime = ScheduleHelper.GetNextRunTime(job, now)
                })
                .Where(result => result.RunTime.HasValue)
                .OrderBy(result => result.RunTime)
                .FirstOrDefault();

            var model = new DashboardViewModel
            {
                ActiveScheduledJobs = activeJobs.Count,
                RegisteredPlugins = _pluginRegistry.GetAll().Count,
                RecentSuccessCount = recentSuccessCount,
                RecentErrorCount = recentErrorCount,
                RecentEntries = recentEntries,
                ActiveJobs = activeJobs.Take(6).ToList(),
                NextUpcomingJob = nextUpcoming?.Job,
                NextUpcomingRunTime = nextUpcoming?.RunTime
            };

            ViewData["Section"] = "Operations";
            return View(model);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
