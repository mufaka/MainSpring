using MainSpringTwo.Web.Models.Plugins;

namespace MainSpringTwo.Web.Models.ViewModels
{
    public class PluginDetailsViewModel
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsRegistered { get; set; }

        public bool IsActive { get; set; }

        public List<PluginParameter> Parameters { get; set; } = [];
    }
}
