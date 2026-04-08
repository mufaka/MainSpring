using MainSpringTwo.Web.Models.Plugins;

namespace MainSpringTwo.Web.Services
{
    public class PluginRegistry
    {
        private readonly Dictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);

        public PluginRegistry(IEnumerable<IPlugin>? plugins = null)
        {
            foreach (var plugin in plugins ?? [])
            {
                Register(plugin);
            }
        }

        public void Register(IPlugin plugin)
        {
            _plugins[plugin.Name] = plugin;
        }

        public IPlugin? GetByName(string name)
        {
            return _plugins.GetValueOrDefault(name);
        }

        public IReadOnlyCollection<IPlugin> GetAll()
        {
            return _plugins.Values.ToArray();
        }
    }
}
