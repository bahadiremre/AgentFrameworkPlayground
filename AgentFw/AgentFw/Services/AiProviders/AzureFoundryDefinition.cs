using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    public sealed class AzureFoundryDefinition : IAiProviderDefinition
    {
        public AiProviderType Type => AiProviderType.AzureAIFoundry;

        public string DisplayName => "Azure AI Foundry";

        public EndpointRequirement Endpoint => EndpointRequirement.Required;

        public IReadOnlyList<AiAuthMode> SupportedAuthModes { get; } = [AiAuthMode.ApiKey, AiAuthMode.EntraId];

        public bool IsApiKeyRequired(AiAuthMode authMode) => authMode == AiAuthMode.ApiKey;

        public string? EndpointHintHtml =>
            "Foundry portalında modelinizin <em>Endpoint</em> alanındaki adres, örn. <code>https://&lt;kaynak&gt;.openai.azure.com/</code>";

        public string? ApiKeyHintHtml => null;

        public string? ChatModelHintHtml =>
            "Foundry'deki <strong>deployment adı</strong> (model adıyla aynı olmayabilir).";
    }
}
