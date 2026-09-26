using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    public interface IAiProviderRegistry
    {
        /// <summary>All definitions in registration order (the order shown in the UI).</summary>
        IReadOnlyList<IAiProviderDefinition> All { get; }

        IAiProviderDefinition? Find(AiProviderType type);

        IAiProviderDefinition Get(AiProviderType type);
    }

    public sealed class AiProviderRegistry : IAiProviderRegistry
    {
        public AiProviderRegistry(IEnumerable<IAiProviderDefinition> definitions)
        {
            All = definitions.ToList();

            var duplicate = All.GroupBy(d => d.Type).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"More than one definition is registered for {duplicate.Key}.");
            }
        }

        public IReadOnlyList<IAiProviderDefinition> All { get; }

        public IAiProviderDefinition? Find(AiProviderType type) => All.FirstOrDefault(d => d.Type == type);

        public IAiProviderDefinition Get(AiProviderType type) =>
            Find(type) ?? throw new InvalidOperationException($"No definition is registered for {type}.");
    }
}
