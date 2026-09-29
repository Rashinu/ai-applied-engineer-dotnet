using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LlmFinOpsCopilot.Infrastructure;

public class LlmDbContextFactory : IDesignTimeDbContextFactory<LlmDbContext>
{
    public LlmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LlmDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres");

        return new LlmDbContext(optionsBuilder.Options);
    }
}