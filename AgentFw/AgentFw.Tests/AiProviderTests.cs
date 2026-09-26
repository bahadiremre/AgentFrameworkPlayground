using System.Net;
using AgentFw.Data;
using AgentFw.Services;
using AgentFw.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgentFw.Tests
{
    [Collection(AppCollection.Name)]
    public class AiProviderTests(AgentFwAppFactory app) : IAsyncLifetime
    {
        private const string ListUrl = "/AiProviders";
        private const string NewUrl = "/AiProviders/Edit";
        private const string TestKey = "test-key-not-real-abcd1234";

        private readonly HttpClient _client = app.CreateBrowserClient();

        public Task InitializeAsync() => app.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        // ---------- Create: validation rules per provider type ----------

        [Fact]
        public async Task Create_Foundry_WithApiKey_StoresEncryptedKeyAndBecomesActive()
        {
            var response = await _client.PostFormAsync(NewUrl, Foundry());

            response.AssertRedirectsTo(ListUrl);
            var saved = await SingleProviderAsync();
            Assert.Equal("Foundry", saved.Name);
            Assert.Equal(AiProviderType.AzureAIFoundry, saved.ProviderType);
            Assert.True(saved.IsActive);
            Assert.Equal("••••1234", saved.ApiKeyHint);
            Assert.NotNull(saved.ProtectedApiKey);
            Assert.DoesNotContain(TestKey, saved.ProtectedApiKey);
            Assert.Equal(TestKey, Unprotect(saved.ProtectedApiKey));
        }

        [Fact]
        public async Task Create_Foundry_WithoutApiKey_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, Foundry(with: new() { ["Input.ApiKey"] = null }));

            await response.AssertShowsErrorAsync("Bu sağlayıcı için API anahtarı zorunlu.");
            Assert.Equal(0, await CountAsync());
        }

        [Fact]
        public async Task Create_Foundry_WithEntraId_DoesNotRequireOrStoreKey()
        {
            var response = await _client.PostFormAsync(NewUrl, Foundry(with: new()
            {
                ["Input.AuthMode"] = "EntraId",
                ["Input.ApiKey"] = TestKey // ignored for Entra ID
            }));

            response.AssertRedirectsTo(ListUrl);
            var saved = await SingleProviderAsync();
            Assert.Equal(AiAuthMode.EntraId, saved.AuthMode);
            Assert.Null(saved.ProtectedApiKey);
            Assert.Null(saved.ApiKeyHint);
        }

        [Fact]
        public async Task Create_Foundry_WithoutEndpoint_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, Foundry(with: new() { ["Input.Endpoint"] = null }));

            await response.AssertShowsErrorAsync("Bu sağlayıcı için endpoint zorunlu.");
            Assert.Equal(0, await CountAsync());
        }

        [Fact]
        public async Task Create_OpenAI_WithoutApiKey_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, OpenAI(with: new() { ["Input.ApiKey"] = null }));

            await response.AssertShowsErrorAsync("Bu sağlayıcı için API anahtarı zorunlu.");
        }

        [Fact]
        public async Task Create_OpenAI_WithoutEndpoint_IsAllowed()
        {
            var response = await _client.PostFormAsync(NewUrl, OpenAI());

            response.AssertRedirectsTo(ListUrl);
            var saved = await SingleProviderAsync();
            Assert.Null(saved.Endpoint);
            Assert.Equal(AiAuthMode.ApiKey, saved.AuthMode);
        }

        [Fact]
        public async Task Create_OpenAICompatible_OnLocalhostHttp_WithoutKey_IsAllowed()
        {
            var response = await _client.PostFormAsync(NewUrl, Ollama());

            response.AssertRedirectsTo(ListUrl);
            var saved = await SingleProviderAsync();
            Assert.Equal("http://localhost:11434/v1", saved.Endpoint);
            Assert.Null(saved.ProtectedApiKey);
        }

        [Fact]
        public async Task Create_OpenAICompatible_OnRemoteHttp_WithoutKey_IsAllowed()
        {
            // Nothing secret is sent, so plain http is acceptable.
            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new() { ["Input.Endpoint"] = "http://example.com/v1" }));

            response.AssertRedirectsTo(ListUrl);
        }

        [Fact]
        public async Task Create_WithKey_OverHttpToRemoteHost_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new()
            {
                ["Input.Endpoint"] = "http://example.com/v1",
                ["Input.ApiKey"] = TestKey
            }));

            await response.AssertShowsErrorAsync("API anahtarı şifresiz (http) bağlantıyla dış bir adrese gönderilemez.");
            Assert.Equal(0, await CountAsync());
        }

        [Fact]
        public async Task Create_WithKey_OverHttpToLocalhost_IsAllowed()
        {
            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new() { ["Input.ApiKey"] = TestKey }));

            response.AssertRedirectsTo(ListUrl);
            Assert.NotNull((await SingleProviderAsync()).ProtectedApiKey);
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("ftp://example.com")]
        public async Task Create_WithInvalidEndpoint_IsRejected(string endpoint)
        {
            var response = await _client.PostFormAsync(NewUrl, Foundry(with: new() { ["Input.Endpoint"] = endpoint }));

            await response.AssertShowsErrorAsync("Geçerli bir http(s) adresi girin.");
        }

        [Fact]
        public async Task Create_WithDuplicateName_IsRejected()
        {
            (await _client.PostFormAsync(NewUrl, Foundry())).AssertRedirectsTo(ListUrl);

            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new() { ["Input.Name"] = "Foundry" }));

            await response.AssertShowsErrorAsync("Bu adla bir sağlayıcı zaten var.");
            Assert.Equal(1, await CountAsync());
        }

        [Fact]
        public async Task Create_WithoutNameOrChatModel_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new()
            {
                ["Input.Name"] = "   ",
                ["Input.ChatModel"] = null
            }));

            var html = await response.ReadDecodedAsync();
            Assert.Contains("Ad zorunlu.", html);
            Assert.Contains("Sohbet modeli zorunlu.", html);
        }

        [Fact]
        public async Task Create_EmbeddingDimensionsWithoutModel_IsRejected()
        {
            var response = await _client.PostFormAsync(NewUrl, Ollama(with: new() { ["Input.EmbeddingDimensions"] = "1536" }));

            await response.AssertShowsErrorAsync("Boyut girmek için önce embedding modelini girin.");
        }

        [Fact]
        public async Task Create_TrimsWhitespace()
        {
            await _client.PostFormAsync(NewUrl, Ollama(with: new()
            {
                ["Input.Name"] = "  Yerel  ",
                ["Input.ChatModel"] = " llama3.2 ",
                ["Input.EmbeddingModel"] = "  "
            }));

            var saved = await SingleProviderAsync();
            Assert.Equal("Yerel", saved.Name);
            Assert.Equal("llama3.2", saved.ChatModel);
            Assert.Null(saved.EmbeddingModel);
        }

        // ---------- Active provider ----------

        [Fact]
        public async Task SecondProvider_IsNotActive()
        {
            await CreateAsync(Foundry());
            await CreateAsync(Ollama());

            var providers = await AllProvidersAsync();
            Assert.True(providers.Single(p => p.Name == "Foundry").IsActive);
            Assert.False(providers.Single(p => p.Name == "Ollama").IsActive);
        }

        [Fact]
        public async Task Activate_MakesOnlyThatProviderActive()
        {
            await CreateAsync(Foundry());
            await CreateAsync(Ollama());
            var ollamaId = (await AllProvidersAsync()).Single(p => p.Name == "Ollama").Id;

            var response = await _client.PostFormAsync(ListUrl, $"{ListUrl}?id={ollamaId}&handler=Activate", new Dictionary<string, string?>());

            response.AssertRedirectsTo(ListUrl);
            var active = (await AllProvidersAsync()).Where(p => p.IsActive).ToList();
            Assert.Single(active);
            Assert.Equal("Ollama", active[0].Name);
        }

        [Fact]
        public async Task Delete_ActiveProvider_LeavesNoActiveProvider()
        {
            // Documents current behavior; the refactor plan revisits this rule.
            await CreateAsync(Foundry());
            await CreateAsync(Ollama());
            var foundryId = (await AllProvidersAsync()).Single(p => p.Name == "Foundry").Id;

            var response = await _client.PostFormAsync(ListUrl, $"{ListUrl}?id={foundryId}&handler=Delete", new Dictionary<string, string?>());

            response.AssertRedirectsTo(ListUrl);
            var remaining = await AllProvidersAsync();
            Assert.Equal("Ollama", Assert.Single(remaining).Name);
            Assert.False(remaining[0].IsActive);
        }

        // ---------- Edit: API key handling ----------

        [Fact]
        public async Task Edit_WithoutNewKey_KeepsStoredKey()
        {
            await CreateAsync(Foundry());
            var before = await SingleProviderAsync();

            var response = await _client.PostFormAsync(EditUrl(before.Id), Foundry(with: new()
            {
                ["Input.ApiKey"] = null,
                ["Input.ChatModel"] = "gpt-4.1-mini"
            }));

            response.AssertRedirectsTo(ListUrl);
            var after = await SingleProviderAsync();
            Assert.Equal("gpt-4.1-mini", after.ChatModel);
            Assert.Equal(before.ProtectedApiKey, after.ProtectedApiKey);
            Assert.Equal(before.ApiKeyHint, after.ApiKeyHint);
        }

        [Fact]
        public async Task Edit_WithNewKey_ReplacesStoredKey()
        {
            await CreateAsync(Foundry());
            var id = (await SingleProviderAsync()).Id;

            await _client.PostFormAsync(EditUrl(id), Foundry(with: new() { ["Input.ApiKey"] = "another-test-key-9876" }));

            var after = await SingleProviderAsync();
            Assert.Equal("••••9876", after.ApiKeyHint);
            Assert.Equal("another-test-key-9876", Unprotect(after.ProtectedApiKey!));
        }

        [Fact]
        public async Task Edit_SwitchingToEntraId_RemovesStoredKey()
        {
            await CreateAsync(Foundry());
            var id = (await SingleProviderAsync()).Id;

            await _client.PostFormAsync(EditUrl(id), Foundry(with: new()
            {
                ["Input.AuthMode"] = "EntraId",
                ["Input.ApiKey"] = null
            }));

            var after = await SingleProviderAsync();
            Assert.Equal(AiAuthMode.EntraId, after.AuthMode);
            Assert.Null(after.ProtectedApiKey);
            Assert.Null(after.ApiKeyHint);
        }

        [Fact]
        public async Task Edit_RemoveApiKey_ClearsOptionalKey()
        {
            await CreateAsync(Ollama(with: new() { ["Input.ApiKey"] = TestKey }));
            var id = (await SingleProviderAsync()).Id;

            await _client.PostFormAsync(EditUrl(id), Ollama(with: new() { ["Input.RemoveApiKey"] = "true" }));

            Assert.Null((await SingleProviderAsync()).ProtectedApiKey);
        }

        [Fact]
        public async Task Edit_RemoveApiKey_WhenKeyIsRequired_IsRejected()
        {
            await CreateAsync(Foundry());
            var id = (await SingleProviderAsync()).Id;

            var response = await _client.PostFormAsync(EditUrl(id), Foundry(with: new()
            {
                ["Input.ApiKey"] = null,
                ["Input.RemoveApiKey"] = "true"
            }));

            await response.AssertShowsErrorAsync("Bu sağlayıcı için API anahtarı zorunlu.");
            Assert.NotNull((await SingleProviderAsync()).ProtectedApiKey);
        }

        [Fact]
        public async Task EditPage_NeverRendersTheStoredKey()
        {
            await CreateAsync(Foundry());
            var id = (await SingleProviderAsync()).Id;

            var html = await (await _client.GetAsync(EditUrl(id))).ReadDecodedAsync();

            Assert.DoesNotContain(TestKey, html);
            Assert.Contains("••••1234", html);
        }

        [Fact]
        public async Task Edit_UnknownId_ReturnsNotFound()
        {
            var response = await _client.GetAsync(EditUrl(999));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ---------- Helpers ----------

        private static string EditUrl(int id) => $"{NewUrl}/{id}";

        private static Dictionary<string, string?> Foundry(Dictionary<string, string?>? with = null) => Merge(new()
        {
            ["Input.Name"] = "Foundry",
            ["Input.ProviderType"] = "AzureAIFoundry",
            ["Input.AuthMode"] = "ApiKey",
            ["Input.Endpoint"] = "https://example-resource.openai.azure.com/",
            ["Input.ApiKey"] = TestKey,
            ["Input.ChatModel"] = "gpt-4o-mini",
            ["Input.EmbeddingModel"] = "text-embedding-3-small",
            ["Input.EmbeddingDimensions"] = "1536"
        }, with);

        private static Dictionary<string, string?> OpenAI(Dictionary<string, string?>? with = null) => Merge(new()
        {
            ["Input.Name"] = "OpenAI",
            ["Input.ProviderType"] = "OpenAI",
            ["Input.ApiKey"] = TestKey,
            ["Input.ChatModel"] = "gpt-4o-mini"
        }, with);

        private static Dictionary<string, string?> Ollama(Dictionary<string, string?>? with = null) => Merge(new()
        {
            ["Input.Name"] = "Ollama",
            ["Input.ProviderType"] = "OpenAICompatible",
            ["Input.Endpoint"] = "http://localhost:11434/v1",
            ["Input.ChatModel"] = "llama3.2"
        }, with);

        private static Dictionary<string, string?> Merge(Dictionary<string, string?> fields, Dictionary<string, string?>? overrides)
        {
            foreach (var (key, value) in overrides ?? [])
            {
                fields[key] = value;
            }
            return fields;
        }

        private async Task CreateAsync(Dictionary<string, string?> fields) =>
            (await _client.PostFormAsync(NewUrl, fields)).AssertRedirectsTo(ListUrl);

        private Task<int> CountAsync() => app.QueryAsync(db => db.AiProviders.CountAsync());

        private Task<AiProvider> SingleProviderAsync() => app.QueryAsync(db => db.AiProviders.AsNoTracking().SingleAsync());

        private Task<List<AiProvider>> AllProvidersAsync() =>
            app.QueryAsync(db => db.AiProviders.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

        private string Unprotect(string protectedKey) =>
            app.Services.GetRequiredService<ApiKeyProtector>().Unprotect(protectedKey);
    }
}
