using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    public enum EndpointRequirement
    {
        Required,
        Optional
    }

    /// <summary>
    /// Everything that differs between provider types lives in one implementation of this interface.
    /// Adding a provider = a new enum value + a new definition class registered in DI.
    /// </summary>
    public interface IAiProviderDefinition
    {
        AiProviderType Type { get; }

        string DisplayName { get; }

        EndpointRequirement Endpoint { get; }

        /// <summary>Supported authentication modes; the first one is the default.</summary>
        IReadOnlyList<AiAuthMode> SupportedAuthModes { get; }

        bool IsApiKeyRequired(AiAuthMode authMode);

        // UI hints. They are trusted constants and may contain simple HTML markup.
        string? EndpointHintHtml { get; }

        string? ApiKeyHintHtml { get; }

        string? ChatModelHintHtml { get; }
    }
}
