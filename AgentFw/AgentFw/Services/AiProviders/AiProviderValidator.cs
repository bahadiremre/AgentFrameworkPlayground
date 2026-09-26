using System.ComponentModel.DataAnnotations;

namespace AgentFw.Services.AiProviders
{
    /// <summary>A validation failure; <see cref="Field"/> is an <see cref="AiProviderInput"/> property name, or null for the whole form.</summary>
    public sealed record ValidationError(string? Field, string Message);

    /// <summary>
    /// Validates a normalized <see cref="AiProviderInput"/>. Type-specific rules come from the
    /// provider definitions; rules that apply to every type are implemented here.
    /// </summary>
    public sealed class AiProviderValidator(IAiProviderRegistry registry)
    {
        public List<ValidationError> Validate(AiProviderInput input, bool hasStoredKey)
        {
            var errors = ValidateAttributes(input);

            var definition = registry.Find(input.ProviderType);
            if (definition is null)
            {
                errors.Add(new(nameof(AiProviderInput.ProviderType), "Geçersiz sağlayıcı türü."));
                return errors;
            }

            var willHaveKey = input.UsesApiKey && (input.ApiKey is not null || hasStoredKey);

            if (definition.Endpoint == EndpointRequirement.Required && input.Endpoint is null)
            {
                errors.Add(new(nameof(AiProviderInput.Endpoint), "Bu sağlayıcı için endpoint zorunlu."));
            }

            if (input.Endpoint is not null)
            {
                if (!Uri.TryCreate(input.Endpoint, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    errors.Add(new(nameof(AiProviderInput.Endpoint), "Geçerli bir http(s) adresi girin."));
                }
                else if (willHaveKey && uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
                {
                    // Never send an API key unencrypted to another machine.
                    errors.Add(new(nameof(AiProviderInput.Endpoint),
                        "API anahtarı şifresiz (http) bağlantıyla dış bir adrese gönderilemez. https kullanın."));
                }
            }

            if (definition.IsApiKeyRequired(input.AuthMode) && !willHaveKey)
            {
                errors.Add(new(nameof(AiProviderInput.ApiKey), "Bu sağlayıcı için API anahtarı zorunlu."));
            }

            // Embedding dimensions only make sense together with an embedding model.
            if (input.EmbeddingDimensions is not null && input.EmbeddingModel is null)
            {
                errors.Add(new(nameof(AiProviderInput.EmbeddingDimensions), "Boyut girmek için önce embedding modelini girin."));
            }

            return errors;
        }

        // Keeps the service safe to call without MVC model binding (which normally runs these).
        private static List<ValidationError> ValidateAttributes(AiProviderInput input)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
            return results
                .Select(r => new ValidationError(r.MemberNames.FirstOrDefault(), r.ErrorMessage ?? "Geçersiz değer."))
                .ToList();
        }
    }
}
