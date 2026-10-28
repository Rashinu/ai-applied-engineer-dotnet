using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReleaseRiskAnalyst.Infrastructure;

public class ReleaseRiskDbContextFactory : IDesignTimeDbContextFactory<ReleaseRiskDbContext>
{
    public ReleaseRiskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReleaseRiskDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5434;Database=releaseriskdb;Username=postgres;Password=postgres");

        return new ReleaseRiskDbContext(optionsBuilder.Options);
    }
}
