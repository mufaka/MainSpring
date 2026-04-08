namespace MainSpringTwo.Web.Models.ViewModels
{
    public class PluginCatalogItemViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int ParameterCount { get; set; }

        public bool IsRegistered { get; set; }

        public bool IsActive { get; set; }
    }
}
