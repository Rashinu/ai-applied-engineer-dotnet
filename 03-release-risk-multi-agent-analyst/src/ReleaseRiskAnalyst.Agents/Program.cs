using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;

// --- 1) MCP sunucusuna bağlan (Faz 3'te yazdığımız ReleaseRiskAnalyst.McpServer) ---
// Inspector'ın yaptığı şeyi burada kod olarak yapıyoruz: sunucu process'ini
// stdio üzerinden başlatıp bağlanıyoruz.
var mcpTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "release-risk-mcp",
    Command = "dotnet",
    Arguments = ["run", "--project", "../ReleaseRiskAnalyst.McpServer/ReleaseRiskAnalyst.McpServer.csproj"],
});

await using var mcpClient = await McpClient.CreateAsync(mcpTransport);

// --- 2) Sunucunun sunduğu tool'ları çek ---
// McpClientTool, Microsoft.Extensions.AI.AIFunction'ın bir alt sınıfı —
// yani dönüşümsüz, doğrudan bir ajana "tools" olarak verilebiliyor.
var tools = await mcpClient.ListToolsAsync();

// --- 3) LLM istemcisi (Proje 2'deki ile aynı desen: OllamaApiClient, IChatClient'ı doğrudan uyguluyor) ---
// HttpClient'ın varsayılan 100 saniyelik timeout'u, ajan + tool-calling (MCP
// round-trip dahil) + CPU üzerinde yavaş inference kombinasyonu için yetersiz
// kaldı (gerçek hata: TaskCanceledException). Timeout'u 5 dakikaya çıkarıyoruz.
var ollamaHttpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11436"),
    Timeout = TimeSpan.FromMinutes(5),
};
IChatClient chatClient = new OllamaApiClient(ollamaHttpClient, "llama3.2:3b");

// --- 4) Ajanı kur ---
// TODO (sen yazacaksın): "instructions" — bu ajanın kimliği ve görevi ne?
// "Kod Risk Ajanı" olarak, release sinyallerine bakıp risk hakkında ne
// yapması gerektiğini anlatan bir talimat metni yaz.
const string instructions =" Sen bir Kod Risk Ajanısın. Bir release'in kod değişiklik sinyallerine bakarak risk değerlendirmesi yaparsın. Release sinyalleri ve servis incident geçmişi verilerini kullanarak, release'in potansiyel risklerini analiz et ve bir risk raporu oluştur. Raporunda, risk seviyesini (düşük, orta, yüksek) belirt ve gerekirse önerilerde bulun.";

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: instructions,
    name: "Kod Risk Ajanı",
    description: "Bir release'in kod değişikliği sinyallerine bakarak risk değerlendirmesi yapar.",
    tools: [.. tools]);

// --- 5) TODO (sen yazacaksın): ---
// Bir AgentSession oluştur (agent.CreateSessionAsync()), ajana bir soru sor
// (agent.RunAsync(message, session)), sonucu ekrana yazdır.

// 3B'lik küçük model "kısa tut" talimatına güvenilir şekilde uymuyor (gerçek
// hata: 1400+ token üretip 300 saniyelik timeout'u aştı). Prompt'la istemek
// yetmiyor, API'nin kendisinde sert bir üst sınır (MaxOutputTokens) koymak
// gerekiyor.
var runOptions = new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = 200 });

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync(
    "payment-service servisi için son release sinyallerini analiz et ve bir risk raporu oluştur.Cevabını en fazla 3-4 cümleyle, kısa ve öz ver.",
    session,
    runOptions);
Console.WriteLine("Ajanın yanıtı:");
Console.WriteLine(response.Text);