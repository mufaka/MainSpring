using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Controllers
{
    public class JobHistoryController : Controller
    {
        private readonly AppDbContext _db;
        private const int PageSize = 25;

        public JobHistoryController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int page = 1, int? scheduledJobId = null)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var baseQuery = _db.JobHistories
                .AsNoTracking()
                .Include(entry => entry.ScheduledJob)
                .Where(entry => entry.RunTime >= cutoff);

            if (scheduledJobId.HasValue)
            {
                baseQuery = baseQuery.Where(entry => entry.ScheduledJobId == scheduledJobId.Value);
            }

            var totalCount = await baseQuery.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var entries = await baseQuery
                .OrderByDescending(entry => entry.RunTime)
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var scheduledJobs = await _db.ScheduledJobs
                .AsNoTracking()
                .OrderBy(job => job.Name)
                .ToListAsync();

            var model = new JobHistoryIndexViewModel
            {
                Entries = entries,
                Page = currentPage,
                TotalPages = totalPages,
                FilterScheduledJobId = scheduledJobId,
                ScheduledJobOptions = new SelectList(scheduledJobs, "ScheduledJobId", "Name", scheduledJobId)
            };

            ViewData["Section"] = "Operational logs";
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var entry = await _db.JobHistories
                .AsNoTracking()
                .Include(history => history.ScheduledJob)
                .ThenInclude(job => job!.Plugin)
                .FirstOrDefaultAsync(history => history.JobHistoryId == id);

            if (entry is null)
            {
                return NotFound();
            }

            ViewData["Section"] = "Operational logs";
            return View(entry);
        }
    }
}
