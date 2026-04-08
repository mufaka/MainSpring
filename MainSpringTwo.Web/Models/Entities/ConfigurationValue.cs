namespace MainSpringTwo.Web.Models.Entities
{
    public class ConfigurationValue
    {
        public int ConfigurationValueId { get; set; }

        public int ScheduledJobId { get; set; }

        public string ParameterName { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public DateTime InsertDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public ScheduledJob? ScheduledJob { get; set; }
    }
}
