namespace MainSpringTwo.Web.Models.Plugins
{
    public interface IPlugin
    {
        string Name { get; }

        string Description { get; }

        List<PluginParameter> ConfigurationParameters { get; }

        Task<PluginResult> RunAsync(
            Dictionary<string, string> configuration,
            CancellationToken cancellationToken);
    }
}
