using AgentFw.Data;
using Microsoft.EntityFrameworkCore;

namespace AgentFw.Services.AiProviders
{
    public sealed class AiProviderService(
        AppDbContext db,
        IAiProviderRegistry registry,
        AiProviderValidator validator,
        IApiKeyProtector protector,
        TimeProvider time) : IAiProviderService
    {
        public async Task<IReadOnlyList<AiProviderSummary>> ListAsync(CancellationToken ct = default)
        {
            var providers = await db.AiProviders
                .AsNoTracking()
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);

            return providers
                .Select(p => new AiProviderSummary(
                    p.Id,
                    p.Name,
                    registry.Find(p.ProviderType)?.DisplayName ?? p.ProviderType.ToString(),
                    p.Endpoint,
                    p.AuthMode,
                    p.ApiKeyHint,
                    p.ChatModel,
                    p.EmbeddingModel,
                    p.EmbeddingDimensions,
                    p.IsActive))
                .ToList();
        }

        public async Task<AiProviderEditState?> GetForEditAsync(int id, CancellationToken ct = default)
        {
            var provider = await db.AiProviders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return provider is null ? null : new AiProviderEditState(AiProviderInput.From(provider), provider.ApiKeyHint);
        }

        public async Task<IReadOnlyList<ValidationError>> ValidateAsync(int? id, AiProviderInput input, CancellationToken ct = default)
        {
            var hasStoredKey = id is not null && await db.AiProviders
                .Where(p => p.Id == id)
                .AnyAsync(p => p.ProtectedApiKey != null, ct);

            return await ValidateCoreAsync(id, input, hasStoredKey, ct);
        }

        public async Task<SaveResult> SaveAsync(int? id, AiProviderInput input, CancellationToken ct = default)
        {
            AiProvider? provider = null;
            if (id is not null)
            {
                provider = await db.AiProviders.FirstOrDefaultAsync(p => p.Id == id, ct);
                if (provider is null)
                {
                    return SaveResult.NotFound();
                }
            }

            var errors = await ValidateCoreAsync(id, input, provider?.ProtectedApiKey is not null, ct);
            if (errors.Count > 0)
            {
                return SaveResult.Invalid(errors);
            }

            var now = time.GetUtcNow().UtcDateTime;
            var created = provider is null;
            if (provider is null)
            {
                provider = new AiProvider
                {
                    CreatedAt = now,
                    // The first provider becomes active automatically.
                    IsActive = !await db.AiProviders.AnyAsync(ct)
                };
                db.AiProviders.Add(provider);
            }

            input.ApplyTo(provider);
            ApplyApiKey(provider, input);
            provider.UpdatedAt = now;

            await db.SaveChangesAsync(ct);
            return SaveResult.Saved(provider.Name, created);
        }

        public async Task<string?> ActivateAsync(int id, CancellationToken ct = default)
        {
            var name = await db.AiProviders.Where(p => p.Id == id).Select(p => p.Name).FirstOrDefaultAsync(ct);
            if (name is null)
            {
                return null;
            }

            // Deactivate first so the "only one active" unique index is never violated.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.AiProviders
                .Where(p => p.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), ct);
            await db.AiProviders
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true), ct);
            await transaction.CommitAsync(ct);

            return name;
        }

        public async Task<string?> DeleteAsync(int id, CancellationToken ct = default)
        {
            var provider = await db.AiProviders.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (provider is null)
            {
                return null;
            }

            // Deleting the active provider leaves no provider active; the user picks the next one.
            db.AiProviders.Remove(provider);
            await db.SaveChangesAsync(ct);
            return provider.Name;
        }

        private async Task<List<ValidationError>> ValidateCoreAsync(int? id, AiProviderInput input, bool hasStoredKey, CancellationToken ct)
        {
            input.Normalize(registry.Find(input.ProviderType));

            var errors = validator.Validate(input, hasStoredKey && !input.RemoveApiKey);

            if (errors.Count == 0 &&
                await db.AiProviders.AnyAsync(p => p.Name == input.Name && p.Id != (id ?? 0), ct))
            {
                errors.Add(new(nameof(AiProviderInput.Name), "Bu adla bir sağlayıcı zaten var."));
            }

            return errors;
        }

        private void ApplyApiKey(AiProvider provider, AiProviderInput input)
        {
            if (!input.UsesApiKey || input.RemoveApiKey)
            {
                // Don't keep secrets that are no longer used (e.g. after switching to Entra ID).
                provider.ProtectedApiKey = null;
                provider.ApiKeyHint = null;
            }

            if (input.UsesApiKey && input.ApiKey is not null)
            {
                provider.ProtectedApiKey = protector.Protect(input.ApiKey);
                provider.ApiKeyHint = ApiKeyProtector.CreateHint(input.ApiKey);
            }
        }
    }
}
