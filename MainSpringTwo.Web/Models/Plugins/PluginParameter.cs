namespace MainSpringTwo.Web.Models.Plugins
{
    public class PluginParameter
    {
        public required string Name { get; init; }

        public string Description { get; init; } = string.Empty;

        public ParameterDataType DataType { get; init; } = ParameterDataType.String;
    }
}
