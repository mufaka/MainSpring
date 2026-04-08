namespace MainSpringTwo.Web.Models.Plugins
{
    public class PluginResult
    {
        public bool Success { get; init; }

        public List<PluginLogEntry> Logs { get; init; } = [];
    }
}
