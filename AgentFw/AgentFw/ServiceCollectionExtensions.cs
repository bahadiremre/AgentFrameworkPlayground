using AgentFw.Data;
using AgentFw.Services;
using AgentFw.Services.AiProviders;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgentFw
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>PostgreSQL via EF Core. The connection string lives in user secrets (ConnectionStrings:AgentRag).</summary>
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("AgentRag"))
                       .UseSnakeCaseNamingConvention());

            return services;
        }

        /// <summary>AI provider settings: type definitions, validation, encrypted key storage and the service.</summary>
        public static IServiceCollection AddAiProviders(this IServiceCollection services)
        {
            // A fixed application name keeps the key ring stable even if the project folder is moved or renamed.
            services.AddDataProtection().SetApplicationName("AgentFrameworkPlayground");
            services.AddSingleton<IApiKeyProtector, ApiKeyProtector>();

            // Registration order is the order shown in the UI.
            services.AddSingleton<IAiProviderDefinition, AzureFoundryDefinition>();
            services.AddSingleton<IAiProviderDefinition, OpenAIDefinition>();
            services.AddSingleton<IAiProviderDefinition, OpenAICompatibleDefinition>();
            services.AddSingleton<IAiProviderRegistry, AiProviderRegistry>();

            services.AddSingleton<AiProviderValidator>();
            services.AddScoped<IAiProviderService, AiProviderService>();
            services.TryAddSingleton(TimeProvider.System);

            return services;
        }

        /// <summary>Applies pending EF Core migrations on startup.</summary>
        public static void ApplyMigrations(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        }
    }
}
