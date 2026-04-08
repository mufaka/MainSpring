namespace MainSpringTwo.Web.Models.Entities
{
    public class ApplicationConfiguration
    {
        public int ApplicationConfigurationId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string ConfigurationName { get; set; } = string.Empty;

        public string ConfigurationValue { get; set; } = string.Empty;

        public DateTime InsertDate { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
