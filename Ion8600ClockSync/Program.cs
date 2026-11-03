using Ion8600ClockSync.Models;
using Ion8600ClockSync.Services;
using Ion8600ClockSync.Services.Clock;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<Dnp3SyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Ion8600SyncConnection")));

builder.Services.AddSingleton<StableClockService>();
builder.Services.AddSingleton<NtpSyncService>();
builder.Services.AddHostedService<NtpClockSyncWorker>();
builder.Services.AddHostedService<MedidorSincronismoWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapGet("/api/medidores/status", async (Dnp3SyncDbContext db) =>
{
    var medidores = await db.Medidores.AsNoTracking().ToListAsync();
    var ultimoPorMedidor = await db.SincronismoHistoricos
        .AsNoTracking()
        .GroupBy(h => h.IdMedidor)
        .Select(g => g.OrderByDescending(h => h.DataHoraUtc).First())
        .ToListAsync();

    var status = medidores.Select(m => new MedidorStatusDto
    {
        Id = m.Id,
        Nome = m.Nome,
        Ip = m.Ip,
        Ativo = m.Ativo,
        DnpHabilitado = m.DnpHabilitado,
        UltimoSync = ultimoPorMedidor.FirstOrDefault(h => h.IdMedidor == m.Id),
    });

    return Results.Ok(status);
});

app.MapRazorPages();

app.Run();
