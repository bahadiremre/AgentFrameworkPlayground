using AgentFw.Services.AiProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgentFw.Pages.AiProviders
{
    public class IndexModel(IAiProviderService providers) : PageModel
    {
        public IReadOnlyList<AiProviderSummary> Providers { get; private set; } = [];

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Providers = await providers.ListAsync(ct);
        }

        public async Task<IActionResult> OnPostActivateAsync(int id, CancellationToken ct)
        {
            if (await providers.ActivateAsync(id, ct) is { } name)
            {
                StatusMessage = $"\"{name}\" artık aktif sağlayıcı.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
        {
            if (await providers.DeleteAsync(id, ct) is { } name)
            {
                StatusMessage = $"\"{name}\" silindi.";
            }
            return RedirectToPage();
        }
    }
}
