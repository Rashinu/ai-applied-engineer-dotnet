using Microsoft.EntityFrameworkCore;
using MarketplaceRegulationAgent.Domain;

namespace MarketplaceRegulationAgent.Infrastructure;

public class RegulationDbContext : DbContext
{
    private readonly Guid _currentTenantId;

    public RegulationDbContext(DbContextOptions<RegulationDbContext> options, Guid currentTenantId)
        : base(options)
    {
        _currentTenantId = currentTenantId;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductValidation> ProductValidations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => p.TenantId == _currentTenantId);

        modelBuilder.Entity<ProductValidation>()
            .HasQueryFilter(v => v.TenantId == _currentTenantId);
    }
}
