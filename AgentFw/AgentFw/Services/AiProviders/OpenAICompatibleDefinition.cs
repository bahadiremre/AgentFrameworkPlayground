using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    /// <summary>Servers that expose the OpenAI API, such as Ollama or LM Studio.</summary>
    public sealed class OpenAICompatibleDefinition : IAiProviderDefinition
    {
        public AiProviderType Type => AiProviderType.OpenAICompatible;

        public string DisplayName => "OpenAI uyumlu (Ollama, LM Studio...)";

        public EndpointRequirement Endpoint => EndpointRequirement.Required;

        public IReadOnlyList<AiAuthMode> SupportedAuthModes { get; } = [AiAuthMode.ApiKey];

        public bool IsApiKeyRequired(AiAuthMode authMode) => false;

        public string? EndpointHintHtml =>
            "Örn. Ollama için <code>http://localhost:11434/v1</code>, LM Studio için <code>http://localhost:1234/v1</code>";

        public string? ApiKeyHintHtml => "Ollama gibi yerel sunucularda genelde gerekmez.";

        public string? ChatModelHintHtml => "Örn. Ollama'da <code>llama3.2</code>";
    }
}
