namespace AgentFw.Services
{
    /// <summary>Encrypts API keys before they are stored and decrypts them when a provider is used.</summary>
    public interface IApiKeyProtector
    {
        string Protect(string apiKey);

        string Unprotect(string protectedApiKey);
    }
}
