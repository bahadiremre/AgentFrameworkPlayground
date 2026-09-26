using AgentFw.Data;

namespace AgentFw.Services.AiProviders
{
    /// <summary>Use cases for managing AI provider settings, independent of the web UI.</summary>
    public interface IAiProviderService
    {
        Task<IReadOnlyList<AiProviderSummary>> ListAsync(CancellationToken ct = default);

        Task<AiProviderEditState?> GetForEditAsync(int id, CancellationToken ct = default);

        /// <summary>Normalizes <paramref name="input"/> in place and returns every validation error.</summary>
        Task<IReadOnlyList<ValidationError>> ValidateAsync(int? id, AiProviderInput input, CancellationToken ct = default);

        /// <summary>Creates (<paramref name="id"/> is null) or updates a provider. Normalizes <paramref name="input"/> in place.</summary>
        Task<SaveResult> SaveAsync(int? id, AiProviderInput input, CancellationToken ct = default);

        /// <summary>Makes the provider the only active one. Returns its name, or null if it doesn't exist.</summary>
        Task<string?> ActivateAsync(int id, CancellationToken ct = default);

        /// <summary>Deletes the provider. Returns its name, or null if it doesn't exist.</summary>
        Task<string?> DeleteAsync(int id, CancellationToken ct = default);
    }

    public sealed record AiProviderSummary(
        int Id,
        string Name,
        string TypeDisplayName,
        string? Endpoint,
        AiAuthMode AuthMode,
        string? ApiKeyHint,
        string ChatModel,
        string? EmbeddingModel,
        int? EmbeddingDimensions,
        bool IsActive);

    /// <summary>Form values for an existing provider plus the hint of its stored key (the key itself is never returned).</summary>
    public sealed record AiProviderEditState(AiProviderInput Input, string? ApiKeyHint);

    public enum SaveStatus
    {
        Saved,
        Invalid,
        NotFound
    }

    public sealed record SaveResult(SaveStatus Status, IReadOnlyList<ValidationError> Errors, string? ProviderName = null, bool Created = false)
    {
        public static SaveResult Saved(string name, bool created) => new(SaveStatus.Saved, [], name, created);

        public static SaveResult Invalid(IReadOnlyList<ValidationError> errors) => new(SaveStatus.Invalid, errors);

        public static SaveResult NotFound() => new(SaveStatus.NotFound, []);
    }
}
