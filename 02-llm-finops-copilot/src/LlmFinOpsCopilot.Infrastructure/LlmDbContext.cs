using Microsoft.EntityFrameworkCore;
using LlmFinOpsCopilotDomain;

namespace LlmFinOpsCopilot.Infrastructure;

public class LlmDbContext : DbContext
{
    public LlmDbContext(DbContextOptions<LlmDbContext> options) : base(options)
    {
    }

    public DbSet<AnomalyWindow> AnomalyWindows { get; set; }

    public DbSet<LlmCallLog> LlmCallLogs { get; set; }
}