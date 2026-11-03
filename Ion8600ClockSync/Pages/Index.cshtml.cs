using Ion8600ClockSync.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ion8600ClockSync.Pages;

public class IndexModel(Dnp3SyncDbContext db) : PageModel
{
    public IList<MedidorStatusDto> Status { get; set; } = new List<MedidorStatusDto>();

    public async Task OnGetAsync()
    {
        var medidores = await db.Medidores.AsNoTracking().OrderBy(m => m.Nome).ToListAsync();
        var ultimoPorMedidor = await db.SincronismoHistoricos
            .AsNoTracking()
            .GroupBy(h => h.IdMedidor)
            .Select(g => g.OrderByDescending(h => h.DataHoraUtc).First())
            .ToListAsync();

        Status = medidores.Select(m => new MedidorStatusDto
        {
            Id = m.Id,
            Nome = m.Nome,
            Ip = m.Ip,
            Ativo = m.Ativo,
            DnpHabilitado = m.DnpHabilitado,
            UltimoSync = ultimoPorMedidor.FirstOrDefault(h => h.IdMedidor == m.Id),
        }).ToList();
    }
}
