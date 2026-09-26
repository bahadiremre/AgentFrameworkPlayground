using AgentFw.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AgentFw.Tests.Infrastructure
{
    /// <summary>
    /// Runs the real app in memory against a throwaway PostgreSQL + pgvector container.
    /// The app applies its migrations on startup, so every test run also verifies them.
    /// </summary>
    public class AgentFwAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
            .WithDatabase("agentrag")
            .WithUsername("agentrag")
            .WithPassword("test-only-password")
            .Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();
            await _postgres.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Not "Development": user secrets (the real connection string) must not be loaded.
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:AgentRag", _postgres.GetConnectionString());

            builder.ConfigureTestServices(services =>
            {
                // Keep encryption keys in memory instead of the user profile.
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }

        public HttpClient CreateBrowserClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        public async Task ResetDatabaseAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("TRUNCATE todos, ai_providers RESTART IDENTITY;");
        }

        public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await query(db);
        }
    }

    [CollectionDefinition(Name)]
    public class AppCollection : ICollectionFixture<AgentFwAppFactory>
    {
        public const string Name = "App";
    }
}
