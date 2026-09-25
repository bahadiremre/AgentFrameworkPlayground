using System.ComponentModel.DataAnnotations;
using AgentFw.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AgentFw.Pages
{
    public class IndexModel(AppDbContext db) : PageModel
    {
        public List<TodoItem> Todos { get; private set; } = [];

        [BindProperty]
        [Required(ErrorMessage = "Görev boş olamaz.")]
        [MaxLength(200, ErrorMessage = "Görev en fazla 200 karakter olabilir.")]
        public string NewTitle { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadTodosAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            NewTitle = NewTitle.Trim();
            if (!ModelState.IsValid || NewTitle.Length == 0)
            {
                await LoadTodosAsync();
                return Page();
            }

            db.Todos.Add(new TodoItem { Title = NewTitle });
            await db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            var todo = await db.Todos.FindAsync(id);
            if (todo is not null)
            {
                todo.IsCompleted = !todo.IsCompleted;
                await db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await db.Todos.Where(t => t.Id == id).ExecuteDeleteAsync();
            return RedirectToPage();
        }

        private async Task LoadTodosAsync()
        {
            Todos = await db.Todos
                .AsNoTracking()
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
