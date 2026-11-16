using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
using ReleaseRiskAnalyst.Api;

var builder = WebApplication.CreateBuilder(args);

// --- MCP sunucusuna bağlan (Faz 3'teki ReleaseRiskAnalyst.McpServer) ---
// Önemli fark: Faz 4-6'daki konsol uygulamasında bu bağlantı HER
// ÇALIŞTIRMADA bir kereydi zaten (program baştan sona bir kere koşuyordu).
// Burada bir web uygulamasıyız — birden fazla HTTP isteği gelecek, bu
// yüzden bağlantıyı burada, uygulama BAŞLARKEN bir kere kurup DI'a
// kaydediyoruz; her istek bu AYNI bağlantıyı/tool listesini paylaşacak.
var mcpTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "release-risk-mcp",
    Command = "dotnet",
    Arguments = ["run", "--project", "../ReleaseRiskAnalyst.McpServer/ReleaseRiskAnalyst.McpServer.csproj"],
});
var mcpClient = await McpClient.CreateAsync(mcpTransport);
var tools = await mcpClient.ListToolsAsync();

var ollamaHttpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11436"),
    Timeout = TimeSpan.FromMinutes(5),
};
IChatClient chatClient = new OllamaApiClient(ollamaHttpClient, "llama3.2:3b");

builder.Services.AddSingleton(chatClient);
builder.Services.AddSingleton<IReadOnlyList<McpClientTool>>((IReadOnlyList<McpClientTool>)tools);
builder.Services.AddSingleton<RiskAnalysisService>();

var app = builder.Build();

// --- TODO (sen yazacaksın) ---
// GET /risk-assessment/{serviceName} endpoint'i. Proje 2'deki GET /insights
// ile aynı kalıp: RiskAnalysisService'i DI'dan parametre olarak al (tıpkı
// Faz 3'teki ReleaseRiskDbContext db parametresi gibi), service.AnalyzeAsync
// (serviceName)'i çağır, sonucu Results.Ok(...) ile dön.


app.MapGet("/risk-assessment/{serviceName}", async (string serviceName, RiskAnalysisService service) =>
{
    var result = await service.AnalyzeAsync(serviceName);
    return Results.Ok(result);
});

app.Run();
