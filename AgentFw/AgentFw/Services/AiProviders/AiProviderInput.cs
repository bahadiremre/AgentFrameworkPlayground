using System.ComponentModel.DataAnnotations;
using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    /// <summary>Data entered on the provider form. Field names are part of the form contract.</summary>
    public class AiProviderInput
    {
        [Required(ErrorMessage = "Ad zorunlu.")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Sağlayıcı türü")]
        public AiProviderType ProviderType { get; set; } = AiProviderType.AzureAIFoundry;

        [Display(Name = "Kimlik doğrulama")]
        public AiAuthMode AuthMode { get; set; } = AiAuthMode.ApiKey;

        [MaxLength(500, ErrorMessage = "Endpoint en fazla 500 karakter olabilir.")]
        [Display(Name = "Endpoint")]
        public string? Endpoint { get; set; }

        // Write-only: never populated from the database.
        [MaxLength(500, ErrorMessage = "API anahtarı çok uzun.")]
        [DataType(DataType.Password)]
        [Display(Name = "API anahtarı")]
        public string? ApiKey { get; set; }

        [Display(Name = "Kayıtlı anahtarı sil")]
        public bool RemoveApiKey { get; set; }

        [Required(ErrorMessage = "Sohbet modeli zorunlu.")]
        [MaxLength(100, ErrorMessage = "Model adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Sohbet modeli")]
        public string ChatModel { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Model adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Embedding modeli")]
        public string? EmbeddingModel { get; set; }

        [Range(1, 16000, ErrorMessage = "Boyut 1 ile 16000 arasında olmalı.")]
        [Display(Name = "Embedding boyutu")]
        public int? EmbeddingDimensions { get; set; }

        public bool UsesApiKey => AuthMode == AiAuthMode.ApiKey;

        /// <summary>Trims values and falls back to the default auth mode if the type doesn't support the chosen one.</summary>
        public void Normalize(IAiProviderDefinition? definition)
        {
            Name = Name?.Trim() ?? string.Empty;
            Endpoint = NullIfEmpty(Endpoint);
            ApiKey = NullIfEmpty(ApiKey);
            ChatModel = ChatModel?.Trim() ?? string.Empty;
            EmbeddingModel = NullIfEmpty(EmbeddingModel);

            if (definition is not null && !definition.SupportedAuthModes.Contains(AuthMode))
            {
                AuthMode = definition.SupportedAuthModes[0];
            }
        }

        public static AiProviderInput From(AiProvider p) => new()
        {
            Name = p.Name,
            ProviderType = p.ProviderType,
            AuthMode = p.AuthMode,
            Endpoint = p.Endpoint,
            ChatModel = p.ChatModel,
            EmbeddingModel = p.EmbeddingModel,
            EmbeddingDimensions = p.EmbeddingDimensions
        };

        public void ApplyTo(AiProvider p)
        {
            p.Name = Name;
            p.ProviderType = ProviderType;
            p.AuthMode = AuthMode;
            p.Endpoint = Endpoint;
            p.ChatModel = ChatModel;
            p.EmbeddingModel = EmbeddingModel;
            p.EmbeddingDimensions = EmbeddingModel is null ? null : EmbeddingDimensions;
        }

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
