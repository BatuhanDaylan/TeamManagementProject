using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace TeamManagement.Tests.Helpers
{
    //Microsoft.Extensions.Configuration.InMemory paketine ihtiyaç duymadan testlerde IConfiguration
    //sağlamak için minimal bir sahte implementasyon; sadece indexer üzerinden okuma desteklenir
    public class FakeConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public FakeConfiguration(Dictionary<string, string?> values)
        {
            _values = values;
        }

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => throw new NotImplementedException();

        public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
    }
}
