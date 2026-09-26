using System.ComponentModel.DataAnnotations;

namespace AgentFw.Data
{
    // Display names and per-type rules live in Services/AiProviders/*Definition.cs.
    public enum AiProviderType
    {
        AzureAIFoundry,
        OpenAI,
        OpenAICompatible
    }

    public enum AiAuthMode
    {
        [Display(Name = "API anahtarı")]
        ApiKey,

        [Display(Name = "Entra ID (Azure girişi)")]
        EntraId
    }

    public class AiProvider
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public AiProviderType ProviderType { get; set; }

        public AiAuthMode AuthMode { get; set; } = AiAuthMode.ApiKey;

        [MaxLength(500)]
        public string? Endpoint { get; set; }

        // Encrypted with ASP.NET Core Data Protection; never stored in plain text.
        public string? ProtectedApiKey { get; set; }

        // Last characters of the key, shown in the UI so the user can tell keys apart.
        [MaxLength(8)]
        public string? ApiKeyHint { get; set; }

        [Required, MaxLength(100)]
        public string ChatModel { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EmbeddingModel { get; set; }

        public int? EmbeddingDimensions { get; set; }

        public bool IsActive { get; set; }

        // Set by AiProviderService through TimeProvider.
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
