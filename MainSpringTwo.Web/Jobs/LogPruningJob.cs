using MainSpringTwo.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Jobs
{
    public class LogPruningJob
    {
        private readonly AppDbContext _db;

        public LogPruningJob(AppDbContext db)
        {
            _db = db;
        }

        public async Task ExecuteAsync()
        {
            var retentionDays = await GetRetentionDaysAsync(CancellationToken.None);
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            await _db.JobHistories
                .Where(h => h.RunTime < cutoff)
                .ExecuteDeleteAsync(CancellationToken.None);
        }

        private async Task<int> GetRetentionDaysAsync(CancellationToken cancellationToken)
        {
            var config = await _db.ApplicationConfigurations
                .FirstOrDefaultAsync(c =>
                    c.CategoryName == "Logging" &&
                    c.ConfigurationName == "RetentionDays", cancellationToken);

            return int.TryParse(config?.ConfigurationValue, out var days)
                ? days
                : 7;
        }
    }
}
