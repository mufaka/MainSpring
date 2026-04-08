using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Models.Plugins;
using MainSpringTwo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Jobs
{
    public class PluginExecutorJob
    {
        private readonly AppDbContext _db;
        private readonly PluginRegistry _registry;
        private readonly ILogger<PluginExecutorJob> _logger;

        public PluginExecutorJob(AppDbContext db, PluginRegistry registry, ILogger<PluginExecutorJob> logger)
        {
            _db = db;
            _registry = registry;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.UtcNow;

            await BackfillNextRunTimeAsync(now);

            var dueJobs = await _db.ScheduledJobs
                .Where(j => j.IsActive && j.NextRunTime.HasValue && j.NextRunTime.Value <= now)
                .Include(j => j.Plugin)
                .Include(j => j.ConfigurationValues)
                .ToListAsync(CancellationToken.None);

            foreach (var job in dueJobs)
            {
                await RunJobAsync(job, now, CancellationToken.None);

                job.NextRunTime = ScheduleHelper.ComputeNextRunTime(job, now);
                await _db.SaveChangesAsync(CancellationToken.None);
            }
        }

        private async Task BackfillNextRunTimeAsync(DateTime now)
        {
            var unscheduledJobs = await _db.ScheduledJobs
                .Where(j => j.IsActive && !j.NextRunTime.HasValue)
                .ToListAsync(CancellationToken.None);

            if (unscheduledJobs.Count == 0)
            {
                return;
            }

            foreach (var job in unscheduledJobs)
            {
                job.NextRunTime = ScheduleHelper.ComputeNextRunTime(job);
            }

            await _db.SaveChangesAsync(CancellationToken.None);
        }

        public async Task RunSingleJobAsync(int scheduledJobId)
        {
            var job = await _db.ScheduledJobs
                .Include(j => j.Plugin)
                .Include(j => j.ConfigurationValues)
                .FirstOrDefaultAsync(j => j.ScheduledJobId == scheduledJobId, CancellationToken.None);

            if (job is null)
            {
                _logger.LogWarning("Scheduled job {ScheduledJobId} was not found for immediate execution.", scheduledJobId);
                return;
            }

            await RunJobAsync(job, DateTime.UtcNow, CancellationToken.None);
        }

        private async Task RunJobAsync(ScheduledJob job, DateTime runTime, CancellationToken cancellationToken)
        {
            var pluginName = job.Plugin?.Name;
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                await WriteHistoryAsync(job.ScheduledJobId, runTime, true, "Scheduled job is missing a plugin mapping.", null, cancellationToken);
                return;
            }

            var plugin = _registry.GetByName(pluginName);
            if (plugin is null)
            {
                await WriteHistoryAsync(job.ScheduledJobId, runTime, true, $"Plugin '{pluginName}' is not registered.", null, cancellationToken);
                return;
            }

            try
            {
                var configuration = job.ConfigurationValues
                    .GroupBy(value => value.ParameterName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);

                var result = await plugin.RunAsync(configuration, cancellationToken);
                await PersistResultAsync(job.ScheduledJobId, runTime, result, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled job {ScheduledJobId} failed while executing plugin {PluginName}.", job.ScheduledJobId, pluginName);
                await WriteHistoryAsync(job.ScheduledJobId, runTime, true, ex.Message, ex.ToString(), cancellationToken);
            }
        }

        private async Task PersistResultAsync(int scheduledJobId, DateTime runTime, PluginResult result, CancellationToken cancellationToken)
        {
            if (result.Logs.Count == 0)
            {
                await WriteHistoryAsync(
                    scheduledJobId,
                    runTime,
                    !result.Success,
                    result.Success ? "Plugin executed successfully." : "Plugin execution completed with errors.",
                    null,
                    cancellationToken);

                return;
            }

            foreach (var log in result.Logs)
            {
                await WriteHistoryAsync(
                    scheduledJobId,
                    runTime,
                    log.IsError || !result.Success,
                    log.Message,
                    log.Detail,
                    cancellationToken);
            }
        }

        private async Task WriteHistoryAsync(
            int scheduledJobId,
            DateTime runTime,
            bool isError,
            string message,
            string? detail,
            CancellationToken cancellationToken)
        {
            _db.JobHistories.Add(new JobHistory
            {
                ScheduledJobId = scheduledJobId,
                RunTime = runTime,
                IsError = isError,
                Message = message,
                Detail = detail
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
