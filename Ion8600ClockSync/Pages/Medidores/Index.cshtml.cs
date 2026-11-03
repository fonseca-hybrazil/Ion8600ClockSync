using Ion8600ClockSync.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ion8600ClockSync.Pages.Medidores;

public class IndexModel(Dnp3SyncDbContext db) : PageModel
{
    public IList<Medidor> Medidores { get; set; } = new List<Medidor>();

    public async Task OnGetAsync()
    {
        Medidores = await db.Medidores.OrderBy(m => m.Nome).ToListAsync();
    }
}
