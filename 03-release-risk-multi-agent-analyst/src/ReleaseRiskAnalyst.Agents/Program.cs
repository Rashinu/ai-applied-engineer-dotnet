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
// İki denemede de (serbest metin + "kesin 2 satır yaz" talimatı) model
// format kuralını yok sayıp veriyi tarif etmeye kaçtı. Çözüm: format
// zorunluluğunu modele değil API'ye bırakmak — tipli yapılandırılmış çıktı
// (CodeRiskAssessment + GetResponseAsync<T>, Proje 2'deki guarded LLM ile
// aynı teknik). Bu sayede instructions artık sadece RİSK MANTIĞINI
// anlatıyor, çıktı formatını anlatmasına gerek yok.
const string instructions = @"Sen bir Kod Risk Ajanısın. Sana verilen release
sinyalleri (linesChanged, reviewerCount, testCoverage, failedCiRunCount)
üzerinden risk değerlendirmesi yaparsın. Kural: linesChanged yüksek,
reviewerCount düşük, testCoverage düşük veya failedCiRunCount yüksekse risk
artar.";

// Mimari plana göre her uzman ajan sadece KENDİ tool'unu görmeli. Faz 4'te
// ajana sunucudaki her iki tool da veriliyordu (tools: [.. tools]) — bu da
// ajanın yanlış tool'u (get_service_incident_histories) çağırmasına yol
// açmıştı. "Kod Risk Ajanı" artık sadece get_release_signals'ı görüyor.
var kodRiskTools = tools.Where(t => t.Name == "get_release_signals").ToList();

AIAgent agent = new ChatClientAgent(
    chatClient,
    instructions: instructions,
    name: "Kod Risk Ajanı",
    description: "Bir release'in kod değişikliği sinyallerine bakarak risk değerlendirmesi yapar.",
    tools: [.. kodRiskTools]);

// --- 5) Ajanı, tipli yapılandırılmış çıktı üretecek şekilde çalıştır ---
// AIAgentExtensions.AsIChatClient: ajanı (tool'ları ve instructions'ı dahil
// olmak üzere) bir IChatClient'a çeviriyor — Proje 2'deki guarded LLM'de
// kullandığımız GetResponseAsync<T> deseni buraya da uygulanabiliyor.
// MaxOutputTokens hâlâ bir güvenlik ağı olarak kalıyor (3B model CPU'da
// yavaş; sınırsız üretime izin vermemek için).
AgentSession session = await agent.CreateSessionAsync();
#pragma warning disable MAAI001 // AsIChatClient henüz deneysel (preview) bir API; bilerek kullanıyoruz.
IChatClient agentAsChatClient = agent.AsIChatClient(session);
#pragma warning restore MAAI001

var response = await agentAsChatClient.GetResponseAsync<CodeRiskAssessment>(
    "payment-service servisinin release sinyallerine bak ve risk değerlendirmesi yap.",
    new ChatOptions { MaxOutputTokens = 450 });

Console.WriteLine("--- Ham yanıt ---");
Console.WriteLine(response.Text);
Console.WriteLine("--- /Ham yanıt ---");

CodeRiskAssessment result = response.Result;
Console.WriteLine($"Risk Seviyesi: {result.Level}");
Console.WriteLine($"Sebep: {result.Reason}");