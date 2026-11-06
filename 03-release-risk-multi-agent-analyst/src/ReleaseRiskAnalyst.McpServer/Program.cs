using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReleaseRiskAnalyst.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio transport, protokol mesajlarını stdout üzerinden taşır.
// Varsayılan konsol logger'ı da stdout'a yazdığı için loglar protokolü bozar;
// bu yüzden logları stderr'e yönlendiriyoruz.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddDbContext<ReleaseRiskDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5434;Database=releaseriskdb;Username=postgres;Password=postgres"));

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithStdioServerTransport();

await builder.Build().RunAsync();
