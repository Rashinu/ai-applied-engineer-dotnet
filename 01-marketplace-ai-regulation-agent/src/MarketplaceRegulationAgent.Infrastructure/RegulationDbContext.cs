using Microsoft.EntityFrameworkCore;
using MarketplaceRegulationAgent.Domain;

namespace MarketplaceRegulationAgent.Infrastructure;

public class RegulationDbContext : DbContext
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public RegulationDbContext(DbContextOptions<RegulationDbContext> options, ICurrentTenantAccessor tenantAccessor)
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductValidation> ProductValidations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Dikkat: _tenantAccessor.TenantId'yi burada bir değişkene KOPYALAMIYORUZ —
        // filtre her sorgu çalıştığında _tenantAccessor'ın O ANKİ değerini okuyor.
        // Worker'da Consume() metodu, DbContext oluşturulduktan SONRA
        // MessageTenantAccessor.TenantId'yi dolduruyor; eğer burada bir kopya
        // tutsaydık, DbContext hep constructor anındaki (boş) değeri görürdü.
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => p.TenantId == _tenantAccessor.TenantId);

        modelBuilder.Entity<ProductValidation>()
            .HasQueryFilter(v => v.TenantId == _tenantAccessor.TenantId);

        modelBuilder.Entity<Product>()
            .Property(p => p.Embedding)
            .HasColumnType("vector(768)");

        modelBuilder.HasPostgresExtension("vector");
    }
}
