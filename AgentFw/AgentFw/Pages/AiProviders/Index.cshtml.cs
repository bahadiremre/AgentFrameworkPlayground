using AgentFw.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AgentFw.Pages.AiProviders
{
    public class IndexModel(AppDbContext db) : PageModel
    {
        public List<AiProvider> Providers { get; private set; } = [];

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            Providers = await db.AiProviders
                .AsNoTracking()
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            var provider = await db.AiProviders.FindAsync(id);
            if (provider is null)
            {
                return RedirectToPage();
            }

            // Deactivate first so the "only one active" unique index is never violated.
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.AiProviders
                .Where(p => p.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
            await db.AiProviders
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true));
            await transaction.CommitAsync();

            StatusMessage = $"\"{provider.Name}\" artık aktif sağlayıcı.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var provider = await db.AiProviders.FindAsync(id);
            if (provider is not null)
            {
                db.AiProviders.Remove(provider);
                await db.SaveChangesAsync();
                StatusMessage = $"\"{provider.Name}\" silindi.";
            }
            return RedirectToPage();
        }
    }
}
