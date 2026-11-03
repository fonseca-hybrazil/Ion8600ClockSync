using Ion8600ClockSync.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ion8600ClockSync.Pages.Medidores;

public class CreateModel(Dnp3SyncDbContext db) : PageModel
{
    [BindProperty]
    public Medidor Medidor { get; set; } = new() { Porta = 20000, EnderecoDnp = 100, Ativo = true };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        db.Medidores.Add(Medidor);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
