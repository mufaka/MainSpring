using MainSpringTwo.Web.Models.Plugins;

namespace MainSpringTwo.Web.Plugins
{
    public class SamplePlugin : IPlugin
    {
        public string Name => "Sample";

        public string Description => "A sample plugin used to validate the Hangfire execution pipeline and configuration-driven UI.";

        public List<PluginParameter> ConfigurationParameters =>
        [
            new()
            {
                Name = "EndpointUrl",
                Description = "Target endpoint used when the sample job runs.",
                DataType = ParameterDataType.String
            },
            new()
            {
                Name = "MaxRetries",
                Description = "How many retry attempts the job should simulate.",
                DataType = ParameterDataType.Integer
            },
            new()
            {
                Name = "UseSandbox",
                Description = "Whether to run in sandbox mode.",
                DataType = ParameterDataType.Boolean
            },
            new()
            {
                Name = "Environment",
                Description = "Target environment used by the sample plugin.",
                DataType = ParameterDataType.List,
                Options =
                [
                    new()
                    {
                        Value = "sandbox",
                        Label = "Sandbox"
                    },
                    new()
                    {
                        Value = "production",
                        Label = "Production"
                    }
                ]
            },
            new()
            {
                Name = "Notes",
                Description = "Additional operator notes for the run.",
                DataType = ParameterDataType.Text
            }
        ];

        public Task<PluginResult> RunAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PluginResult
            {
                Success = true,
                Logs =
                [
                    new PluginLogEntry
                    {
                        IsError = false,
                        Message = $"Hello from SamplePlugin ({configuration.GetValueOrDefault("Environment", "sandbox")})"
                    }
                ]
            });
        }
    }
}
