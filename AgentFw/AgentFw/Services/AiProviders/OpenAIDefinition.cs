using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    public sealed class OpenAIDefinition : IAiProviderDefinition
    {
        public AiProviderType Type => AiProviderType.OpenAI;

        public string DisplayName => "OpenAI";

        public EndpointRequirement Endpoint => EndpointRequirement.Optional;

        public IReadOnlyList<AiAuthMode> SupportedAuthModes { get; } = [AiAuthMode.ApiKey];

        public bool IsApiKeyRequired(AiAuthMode authMode) => true;

        public string? EndpointHintHtml =>
            "İsteğe bağlı. Boş bırakırsanız <code>https://api.openai.com/v1</code> kullanılır.";

        public string? ApiKeyHintHtml => null;

        public string? ChatModelHintHtml => null;
    }
}
