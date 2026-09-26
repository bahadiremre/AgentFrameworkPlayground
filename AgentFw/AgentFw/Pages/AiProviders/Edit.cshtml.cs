using System.ComponentModel.DataAnnotations;
using AgentFw.Data;
using AgentFw.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AgentFw.Pages.AiProviders
{
    public class EditModel(AppDbContext db, ApiKeyProtector protector) : PageModel
    {
        [BindProperty]
        public ProviderInput Input { get; set; } = new();

        public bool IsNew { get; private set; }

        // Hint of the key already stored for this provider (e.g. "••••abcd"), if any.
        public string? ExistingKeyHint { get; private set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id is null)
            {
                IsNew = true;
                return Page();
            }

            var provider = await db.AiProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (provider is null)
            {
                return NotFound();
            }

            Input = ProviderInput.From(provider);
            ExistingKeyHint = provider.ApiKeyHint;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            AiProvider? provider = null;
            if (id is not null)
            {
                provider = await db.AiProviders.FirstOrDefaultAsync(p => p.Id == id);
                if (provider is null)
                {
                    return NotFound();
                }
            }

            IsNew = provider is null;
            ExistingKeyHint = provider?.ApiKeyHint;
            Input.Normalize();

            var hasStoredKey = provider?.ProtectedApiKey is not null && !Input.RemoveApiKey;
            ValidateInput(hasStoredKey);

            if (ModelState.IsValid &&
                await db.AiProviders.AnyAsync(p => p.Name == Input.Name && p.Id != (id ?? 0)))
            {
                ModelState.AddModelError("Input.Name", "Bu adla bir sağlayıcı zaten var.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (provider is null)
            {
                provider = new AiProvider
                {
                    // The first provider becomes active automatically.
                    IsActive = !await db.AiProviders.AnyAsync()
                };
                db.AiProviders.Add(provider);
            }

            Input.ApplyTo(provider);
            ApplyApiKey(provider);
            provider.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            StatusMessage = IsNew ? $"\"{provider.Name}\" eklendi." : $"\"{provider.Name}\" güncellendi.";
            return RedirectToPage("Index");
        }

        private void ValidateInput(bool hasStoredKey)
        {
            var usesApiKey = Input.UsesApiKey;
            var willHaveKey = !string.IsNullOrEmpty(Input.ApiKey) || hasStoredKey;

            // Endpoint rules per provider type.
            if ((Input.ProviderType is AiProviderType.AzureAIFoundry or AiProviderType.OpenAICompatible) &&
                string.IsNullOrEmpty(Input.Endpoint))
            {
                ModelState.AddModelError("Input.Endpoint", "Bu sağlayıcı için endpoint zorunlu.");
            }

            if (!string.IsNullOrEmpty(Input.Endpoint))
            {
                if (!Uri.TryCreate(Input.Endpoint, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    ModelState.AddModelError("Input.Endpoint", "Geçerli bir http(s) adresi girin.");
                }
                else if (usesApiKey && willHaveKey && uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
                {
                    // Never send an API key unencrypted to another machine.
                    ModelState.AddModelError("Input.Endpoint",
                        "API anahtarı şifresiz (http) bağlantıyla dış bir adrese gönderilemez. https kullanın.");
                }
            }

            // API key rules per provider type.
            var keyRequired = Input.ProviderType == AiProviderType.OpenAI ||
                              (Input.ProviderType == AiProviderType.AzureAIFoundry && Input.AuthMode == AiAuthMode.ApiKey);
            if (keyRequired && !willHaveKey)
            {
                ModelState.AddModelError("Input.ApiKey", "Bu sağlayıcı için API anahtarı zorunlu.");
            }

            // Embedding dimensions only make sense together with an embedding model.
            if (Input.EmbeddingDimensions is not null && string.IsNullOrEmpty(Input.EmbeddingModel))
            {
                ModelState.AddModelError("Input.EmbeddingDimensions", "Boyut girmek için önce embedding modelini girin.");
            }
        }

        private void ApplyApiKey(AiProvider provider)
        {
            if (!Input.UsesApiKey || Input.RemoveApiKey)
            {
                // Don't keep secrets that are no longer used (e.g. after switching to Entra ID).
                provider.ProtectedApiKey = null;
                provider.ApiKeyHint = null;
            }

            if (Input.UsesApiKey && !string.IsNullOrEmpty(Input.ApiKey))
            {
                provider.ProtectedApiKey = protector.Protect(Input.ApiKey);
                provider.ApiKeyHint = ApiKeyProtector.CreateHint(Input.ApiKey);
            }
        }

        public class ProviderInput
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

            // Only Foundry offers Entra ID; the other types always use a key (optional for OpenAI-compatible).
            public bool UsesApiKey => ProviderType != AiProviderType.AzureAIFoundry || AuthMode == AiAuthMode.ApiKey;

            public void Normalize()
            {
                Name = Name?.Trim() ?? string.Empty;
                Endpoint = NullIfEmpty(Endpoint);
                ApiKey = NullIfEmpty(ApiKey);
                ChatModel = ChatModel?.Trim() ?? string.Empty;
                EmbeddingModel = NullIfEmpty(EmbeddingModel);
                if (ProviderType != AiProviderType.AzureAIFoundry)
                {
                    AuthMode = AiAuthMode.ApiKey;
                }
            }

            public static ProviderInput From(AiProvider p) => new()
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
}
