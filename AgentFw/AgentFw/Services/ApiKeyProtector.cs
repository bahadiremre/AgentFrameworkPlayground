using Microsoft.AspNetCore.DataProtection;

namespace AgentFw.Services
{
    /// <summary>
    /// Encrypts AI provider API keys before they are written to the database.
    /// The Data Protection key ring lives in the user profile, outside the repo and the database.
    /// </summary>
    public class ApiKeyProtector(IDataProtectionProvider provider)
    {
        private readonly IDataProtector _protector = provider.CreateProtector("AgentFw.AiProvider.ApiKey.v1");

        public string Protect(string apiKey) => _protector.Protect(apiKey);

        public string Unprotect(string protectedApiKey) => _protector.Unprotect(protectedApiKey);

        public static string CreateHint(string apiKey) =>
            apiKey.Length <= 8 ? "••••" : "••••" + apiKey[^4..];
    }
}
