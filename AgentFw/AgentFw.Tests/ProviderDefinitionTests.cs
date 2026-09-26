using AgentFw.Data;
using AgentFw.Services.AiProviders;
using AgentFw.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFw.Tests
{
    [Collection(AppCollection.Name)]
    public class ProviderDefinitionTests(AgentFwAppFactory app)
    {
        private IAiProviderRegistry Registry => app.Services.GetRequiredService<IAiProviderRegistry>();

        [Fact]
        public void EveryProviderType_HasExactlyOneDefinition()
        {
            // Fails when a new enum value is added without a matching *Definition class.
            foreach (var type in Enum.GetValues<AiProviderType>())
            {
                Assert.Single(Registry.All, d => d.Type == type);
            }
        }

        [Fact]
        public void EveryDefinition_SupportsAtLeastOneAuthMode()
        {
            Assert.All(Registry.All, d => Assert.NotEmpty(d.SupportedAuthModes));
        }

        [Fact]
        public void Registry_RejectsDuplicateDefinitions()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new AiProviderRegistry([new OpenAIDefinition(), new OpenAIDefinition()]));

            Assert.Contains("OpenAI", ex.Message);
        }

        [Fact]
        public async Task EditPage_PostsTypeNamesNotEnumIndexes()
        {
            // Posting names keeps the form correct if enum values are reordered.
            var html = await (await app.CreateBrowserClient().GetAsync("/AiProviders/Edit")).ReadDecodedAsync();

            foreach (var definition in Registry.All)
            {
                Assert.Contains($"value=\"{definition.Type}\"", html);
                Assert.Contains(definition.DisplayName, html);
            }
        }
    }
}
