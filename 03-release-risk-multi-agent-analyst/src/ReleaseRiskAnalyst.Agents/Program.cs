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

// --- 4) Üç uzman ajanın ortak çalıştırma mantığı ---
// Kod Risk Ajanı'nda kanıtlanan deseni (tool kısıtlama + tipli yapılandırılmış
// çıktı) tek bir fonksiyonda topluyoruz ki üç ajan için de aynı kodu kopyalamayalım.
// Local function top-level statement'lar arasında herhangi bir yere yazılabiliyor
// (Faz 2'de gördüğümüz CS8803 kuralı — bu, class/enum gibi bir tip tanımı değil).
async Task<RiskAssessment> RunRiskAgentAsync(
    string name, string description, string instructions, string toolName, string question)
{
    var agentTools = tools.Where(t => t.Name == toolName).ToList();

    AIAgent agent = new ChatClientAgent(
        chatClient,
        instructions: instructions,
        name: name,
        description: description,
        tools: [.. agentTools]);

    AgentSession session = await agent.CreateSessionAsync();
#pragma warning disable MAAI001 // AsIChatClient henüz deneysel (preview) bir API; bilerek kullanıyoruz.
    IChatClient agentAsChatClient = agent.AsIChatClient(session);
#pragma warning restore MAAI001

    var response = await agentAsChatClient.GetResponseAsync<RiskAssessment>(
        question,
        new ChatOptions { MaxOutputTokens = 450 });

    Console.WriteLine($"--- {name} ham yanıt ---");
    Console.WriteLine(response.Text);
    return response.Result;
}

// --- 5) Kod Risk Ajanı (Faz 4-5'te kuruldu, doğrulandı) ---
const string kodRiskInstructions = @"Sen bir Kod Risk Ajanısın. Sana verilen release
sinyalleri (linesChanged, reviewerCount, testCoverage, failedCiRunCount)
üzerinden risk değerlendirmesi yaparsın. Kural: linesChanged yüksek,
reviewerCount düşük, testCoverage düşük veya failedCiRunCount yüksekse risk
artar.LinesChanged 1000'in üzeri yükse risk, reviewerCount 2'nin altı düşük risk, testCoverage 50%'nin altı düşük risk, failedCiRunCount 5'in üzeri yüksek risk. Risk seviyesini Low, Medium veya High olarak değerlendir ve sebebini açıkla.";

var kodRiskResult = await RunRiskAgentAsync(
    name: "Kod Risk Ajanı",
    description: "Bir release'in kod değişikliği sinyallerine bakarak risk değerlendirmesi yapar.",
    instructions: kodRiskInstructions,
    toolName: "get_release_signals",
    question: "payment-service servisinin release sinyallerine bak ve risk değerlendirmesi yap.");

// --- 6) Test Risk Ajanı ---
// TODO (sen yazacaksın): testCoverage ve failedCiRunCount'a odaklanan bir
// instructions metni — Kod Risk Ajanı'nınkiyle aynı kalıpta (hangi sinyal
// artınca/azalınca risk artar).
const string testRiskInstructions = "Test Coverage ve failedCiRunCount sinyallerine odaklanan bir risk değerlendirmesi yap. Kural: testCoverage düşük veya failedCiRunCount yüksekse risk artar.";

var testRiskResult = await RunRiskAgentAsync(
    name: "Test Risk Ajanı",
    description: "Bir release'in test coverage ve CI sinyallerine bakarak risk değerlendirmesi yapar.",
    instructions: testRiskInstructions,
    toolName: "get_release_signals",
    question: "payment-service servisinin test coverage ve CI sinyallerine bak ve risk değerlendirmesi yap.");

// --- 7) Geçmiş Risk Ajanı ---
// TODO (sen yazacaksın): bu ajan artık release sinyallerine değil, servisin
// GEÇMİŞ INCIDENT kayıtlarına (severity: Low/Medium/High/Critical) bakıyor —
// farklı bir tool (get_service_incident_histories), farklı bir risk mantığı
// (çok sayıda/ağır severity'li incident = yüksek risk).
const string gecmisRiskInstructions = "Bir servisin geçmiş incident kayıtlarına bakarak risk değerlendirmesi yap. Kural: çok sayıda veya ağır severity'li incident = yüksek risk.";

var gecmisRiskResult = await RunRiskAgentAsync(
    name: "Geçmiş Risk Ajanı",
    description: "Bir servisin geçmiş incident kayıtlarına bakarak risk değerlendirmesi yapar.",
    instructions: gecmisRiskInstructions,
    toolName: "get_service_incident_histories",
    question: "payment-service servisinin incident geçmişine bak ve risk değerlendirmesi yap.");

// --- 8) Üç ajanın sonucunu yazdır ---
Console.WriteLine();
Console.WriteLine($"Kod Risk Ajanı       -> Risk Seviyesi: {kodRiskResult.Level} | Sebep: {kodRiskResult.Reason}");
Console.WriteLine($"Test Risk Ajanı      -> Risk Seviyesi: {testRiskResult.Level} | Sebep: {testRiskResult.Reason}");
Console.WriteLine($"Geçmiş Risk Ajanı    -> Risk Seviyesi: {gecmisRiskResult.Level} | Sebep: {gecmisRiskResult.Reason}");
