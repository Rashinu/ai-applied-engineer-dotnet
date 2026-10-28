using Microsoft.EntityFrameworkCore;
using ReleaseRiskAnalyst.Domain;

namespace ReleaseRiskAnalyst.Infrastructure;

public class ReleaseRiskDbContext : DbContext
{
    public ReleaseRiskDbContext(DbContextOptions<ReleaseRiskDbContext> options) : base(options)
    {
    }

    public DbSet<ReleaseSignal> ReleaseSignals { get; set; }
    public DbSet<ServiceIncidentHistory> ServiceIncidentHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // EF Core, "Id" ya da "<SınıfAdı>Id" adlı alanları otomatik birincil anahtar sayar.
        // SignalId/IncidentId bu kalıba uymadığı için açıkça belirtmemiz gerekiyor.
        modelBuilder.Entity<ReleaseSignal>().HasKey(r => r.SignalId);
        modelBuilder.Entity<ServiceIncidentHistory>().HasKey(s => s.IncidentId);
    }
}
