namespace MainSpringTwo.Web.Models.Entities
{
    public class Plugin
    {
        public int PluginId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime InsertDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public ICollection<ScheduledJob> ScheduledJobs { get; set; } = new List<ScheduledJob>();
    }
}
