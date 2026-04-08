namespace MainSpringTwo.Web.Models.Entities
{
    public class ScheduledJob
    {
        public int ScheduledJobId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int PluginId { get; set; }

        public ScheduleType ScheduleType { get; set; }

        public int Interval { get; set; }

        public DateTime StartTime { get; set; }

        public bool IsActive { get; set; }

        public DateTime? NextRunTime { get; set; }

        public DateTime InsertDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public Plugin? Plugin { get; set; }

        public ICollection<ConfigurationValue> ConfigurationValues { get; set; } = new List<ConfigurationValue>();

        public ICollection<JobHistory> JobHistories { get; set; } = new List<JobHistory>();
    }
}
