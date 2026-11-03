using Ion8600ClockSync.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ion8600ClockSync.Pages.Medidores;

public class DeleteModel(Dnp3SyncDbContext db) : PageModel
{
    [BindProperty]
    public Medidor Medidor { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var medidor = await db.Medidores.FindAsync(id);
        if (medidor is null)
            return NotFound();

        Medidor = medidor;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var medidor = await db.Medidores.FindAsync(id);
        if (medidor is not null)
        {
            db.Medidores.Remove(medidor);
            await db.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
