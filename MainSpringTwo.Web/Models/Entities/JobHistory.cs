namespace MainSpringTwo.Web.Models.Entities
{
    public class JobHistory
    {
        public int JobHistoryId { get; set; }

        public int ScheduledJobId { get; set; }

        public DateTime RunTime { get; set; }

        public bool IsError { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public ScheduledJob? ScheduledJob { get; set; }
    }
}
