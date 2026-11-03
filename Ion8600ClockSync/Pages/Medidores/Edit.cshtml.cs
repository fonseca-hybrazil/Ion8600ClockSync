using Ion8600ClockSync.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ion8600ClockSync.Pages.Medidores;

public class EditModel(Dnp3SyncDbContext db) : PageModel
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        db.Attach(Medidor).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
