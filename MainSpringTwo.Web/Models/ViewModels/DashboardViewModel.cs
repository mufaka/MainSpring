using MainSpringTwo.Web.Models.Entities;

namespace MainSpringTwo.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int ActiveScheduledJobs { get; set; }

        public int RegisteredPlugins { get; set; }

        public int RecentSuccessCount { get; set; }

        public int RecentErrorCount { get; set; }

        public List<JobHistory> RecentEntries { get; set; } = [];

        public List<ScheduledJob> ActiveJobs { get; set; } = [];

        public ScheduledJob? NextUpcomingJob { get; set; }

        public DateTime? NextUpcomingRunTime { get; set; }
    }
}
