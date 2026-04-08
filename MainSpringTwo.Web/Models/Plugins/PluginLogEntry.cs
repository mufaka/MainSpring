namespace MainSpringTwo.Web.Models.Plugins
{
    public class PluginLogEntry
    {
        public bool IsError { get; init; }

        public required string Message { get; init; }

        public string? Detail { get; init; }
    }
}
