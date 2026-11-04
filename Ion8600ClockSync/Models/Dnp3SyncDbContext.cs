using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ion8600ClockSync.Models;

public class Dnp3SyncDbContext : DbContext
{
    public Dnp3SyncDbContext(DbContextOptions<Dnp3SyncDbContext> options) : base(options)
    {
    }

    public DbSet<Medidor> Medidores { get; set; } = null!;

    public DbSet<SincronismoHistorico> SincronismoHistoricos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medidor>().ToTable("medidor");
        modelBuilder.Entity<SincronismoHistorico>().ToTable("sincronismo_historico");

        modelBuilder.Entity<SincronismoHistorico>()
            .HasOne(h => h.IdMedidorNavigation)
            .WithMany(m => m.SincronismoHistoricos)
            .HasForeignKey(h => h.IdMedidor)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_sincronismo_historico_medidor");

        // SQL Server não guarda timezone - toda coluna DateTime aqui representa um instante UTC,
        // mas o EF Core devolve DateTimeKind.Unspecified na leitura. Sem Kind=Utc, o
        // System.Text.Json serializa sem o sufixo "Z" e o JS do dashboard acaba interpretando a
        // string como hora local da máquina de quem está vendo a tela, em vez de UTC.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcConverter);
            }
        }
    }
}
