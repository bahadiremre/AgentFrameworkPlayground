using AgentFw.Services.AiProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentFw.Pages.AiProviders
{
    public class EditModel(IAiProviderService providers, IAiProviderRegistry registry) : PageModel
    {
        [BindProperty]
        public AiProviderInput Input { get; set; } = new();

        public bool IsNew { get; private set; }

        // Hint of the key already stored for this provider (e.g. "••••abcd"), if any.
        public string? ExistingKeyHint { get; private set; }

        public IReadOnlyList<IAiProviderDefinition> Definitions => registry.All;

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
        {
            IsNew = id is null;
            if (id is null)
            {
                return Page();
            }

            var state = await providers.GetForEditAsync(id.Value, ct);
            if (state is null)
            {
                return NotFound();
            }

            Input = state.Input;
            ExistingKeyHint = state.ApiKeyHint;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, CancellationToken ct)
        {
            IsNew = id is null;

            // Model binding errors (e.g. a non-numeric dimension) stop the save, but the
            // business rules still run so the user sees every problem at once.
            if (ModelState.IsValid)
            {
                var result = await providers.SaveAsync(id, Input, ct);
                switch (result.Status)
                {
                    case SaveStatus.NotFound:
                        return NotFound();
                    case SaveStatus.Saved:
                        StatusMessage = result.Created
                            ? $"\"{result.ProviderName}\" eklendi."
                            : $"\"{result.ProviderName}\" güncellendi.";
                        return RedirectToPage("Index");
                    default:
                        AddErrors(result.Errors);
                        break;
                }
            }
            else
            {
                AddErrors(await providers.ValidateAsync(id, Input, ct));
            }

            if (id is not null)
            {
                var state = await providers.GetForEditAsync(id.Value, ct);
                if (state is null)
                {
                    return NotFound();
                }
                ExistingKeyHint = state.ApiKeyHint;
            }

            return Page();
        }

        private void AddErrors(IEnumerable<ValidationError> errors)
        {
            foreach (var error in errors)
            {
                var key = error.Field is null ? string.Empty : $"{nameof(Input)}.{error.Field}";

                // Attribute errors may already be in ModelState from model binding.
                var alreadyAdded = ModelState.TryGetValue(key, out var entry) &&
                                   entry.Errors.Any(e => e.ErrorMessage == error.Message);
                if (!alreadyAdded)
                {
                    ModelState.AddModelError(key, error.Message);
                }
            }
        }
    }
}
