using Microsoft.EntityFrameworkCore;

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
    }
}
