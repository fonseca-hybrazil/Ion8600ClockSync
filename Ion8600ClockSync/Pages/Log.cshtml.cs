using Ion8600ClockSync.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ion8600ClockSync.Pages;

public class LogModel(SincronismoLogWriter syncLog) : PageModel
{
    public IReadOnlyList<string> Linhas { get; set; } = [];

    public async Task OnGetAsync()
    {
        Linhas = await syncLog.ReadLastLinesAsync(maxLinhas: 500);
    }
}
