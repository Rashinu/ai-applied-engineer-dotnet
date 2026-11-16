using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace ReleaseRiskAnalyst.Api;

// Faz 4-6'da (ReleaseRiskAnalyst.Agents konsol uygulamasında) kanıtlanan
// mantığın taşınmış hali — tek fark, sabit "payment-service" yerine artık
// herhangi bir serviceName parametresi kabul ediyor. DI üzerinden
// kaydediliyor (Program.cs'e bak), yani IChatClient ve tool listesi
// uygulama başlarken bir kere kurulup buraya enjekte ediliyor.
public class RiskAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly IReadOnlyList<McpClientTool> _tools;

    public RiskAnalysisService(IChatClient chatClient, IReadOnlyList<McpClientTool> tools)
    {
        _chatClient = chatClient;
        _tools = tools;
    }

    private async Task<RiskAssessment> RunRiskAgentAsync(
        string name, string description, string instructions, string toolName, string question)
    {
        var agentTools = _tools.Where(t => t.Name == toolName).ToList();

        AIAgent agent = new ChatClientAgent(
            _chatClient,
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

        return response.Result;
    }

    // Faz 6'da öğrendiğimiz ders: kararın kendisini modelden değil koddan
    // iste. Üç Level değerini karşılaştırıp nihai kararı deterministik
    // olarak hesaplıyoruz.
    private static (RiskAssessment.RiskLevel Level, string BasedOnAgents) ComputeFinalVerdict(
        (string Name, RiskAssessment.RiskLevel Level) kod,
        (string Name, RiskAssessment.RiskLevel Level) test,
        (string Name, RiskAssessment.RiskLevel Level) gecmis)
    {
        var all = new[] { kod, test, gecmis };
        int highCount = all.Count(a => a.Level == RiskAssessment.RiskLevel.High);
        int mediumCount = all.Count(a => a.Level == RiskAssessment.RiskLevel.Medium);

        var finalLevel = highCount >= 2
            ? RiskAssessment.RiskLevel.High
            : (highCount >= 1 || mediumCount >= 1)
                ? RiskAssessment.RiskLevel.Medium
                : RiskAssessment.RiskLevel.Low;

        var basedOn = all.Where(a => a.Level == finalLevel).Select(a => a.Name).ToList();
        if (basedOn.Count == 0)
            basedOn = all.Where(a => a.Level != RiskAssessment.RiskLevel.Low).Select(a => a.Name).ToList();

        return (finalLevel, string.Join(" ve ", basedOn));
    }

    // Dışarıya açılan tek metot: bir servis adı verildiğinde üç uzman
    // ajanı + orkestratörü çalıştırıp nihai, kanıta dayalı kararı döner.
    public async Task<RiskAnalysisResult> AnalyzeAsync(string serviceName)
    {
        const string kodRiskInstructions = @"Sen bir Kod Risk Ajanısın. Sana verilen release
sinyalleri (linesChanged, reviewerCount, testCoverage, failedCiRunCount)
üzerinden risk değerlendirmesi yaparsın. Kural: linesChanged 1000'in üzeri
yüksek risk, reviewerCount 2'nin altı düşük risk, testCoverage %50'nin
altı düşük risk, failedCiRunCount 5'in üzeri yüksek risk.";

        const string testRiskInstructions = @"Test Coverage ve failedCiRunCount
sinyallerine odaklanan bir risk değerlendirmesi yap. Kural: testCoverage
%50'nin altı düşük risk, failedCiRunCount 5'in üzeri yüksek risk.";

        const string gecmisRiskInstructions = @"Bir servisin geçmiş incident
kayıtlarına bakarak risk değerlendirmesi yap. Kural: çok sayıda veya ağır
severity'li (High/Critical) incident = yüksek risk.";

        var kodRiskResult = await RunRiskAgentAsync(
            name: "Kod Risk Ajanı",
            description: "Bir release'in kod değişikliği sinyallerine bakarak risk değerlendirmesi yapar.",
            instructions: kodRiskInstructions,
            toolName: "get_release_signals",
            question: $"{serviceName} servisinin release sinyallerine bak ve risk değerlendirmesi yap.");

        var testRiskResult = await RunRiskAgentAsync(
            name: "Test Risk Ajanı",
            description: "Bir release'in test coverage ve CI sinyallerine bakarak risk değerlendirmesi yapar.",
            instructions: testRiskInstructions,
            toolName: "get_release_signals",
            question: $"{serviceName} servisinin test coverage ve CI sinyallerine bak ve risk değerlendirmesi yap.");

        var gecmisRiskResult = await RunRiskAgentAsync(
            name: "Geçmiş Risk Ajanı",
            description: "Bir servisin geçmiş incident kayıtlarına bakarak risk değerlendirmesi yapar.",
            instructions: gecmisRiskInstructions,
            toolName: "get_service_incident_histories",
            question: $"{serviceName} servisinin incident geçmişine bak ve risk değerlendirmesi yap.");

        var (finalLevel, basedOnAgents) = ComputeFinalVerdict(
            ("Kod Risk Ajanı", kodRiskResult.Level),
            ("Test Risk Ajanı", testRiskResult.Level),
            ("Geçmiş Risk Ajanı", gecmisRiskResult.Level));

        const string orkestratorInstructions = @"Sen bir Orkestratör ajanısın. Sana üç uzman
ajanın (Kod Risk, Test Risk, Geçmiş Risk) risk seviyesi, sebebi ve zaten
verilmiş olan NİHAİ risk seviyesi verilecek. Görevin bu nihai kararı,
hangi ajan(lar)ın bulgusuna dayandığını da belirterek, kısa bir paragrafla
açıklamak. Kararı SEN vermiyorsun, sadece zaten verilmiş kararı açıklıyorsun.";

        AIAgent orkestratorAgent = new ChatClientAgent(
            _chatClient,
            instructions: orkestratorInstructions,
            name: "Orkestratör",
            description: "Üç uzman ajanın risk bulgularını birleştirip nihai kararı gerekçelendirir.",
            tools: []);

        AgentSession orkestratorSession = await orkestratorAgent.CreateSessionAsync();
#pragma warning disable MAAI001
        IChatClient orkestratorAsChatClient = orkestratorAgent.AsIChatClient(orkestratorSession);
#pragma warning restore MAAI001

        string orkestratorPrompt = $@"
Kod Risk Ajanı: Seviye={kodRiskResult.Level}, Sebep={kodRiskResult.Reason}
Test Risk Ajanı: Seviye={testRiskResult.Level}, Sebep={testRiskResult.Reason}
Geçmiş Risk Ajanı: Seviye={gecmisRiskResult.Level}, Sebep={gecmisRiskResult.Reason}
NİHAİ KARAR (zaten verildi): {finalLevel}, dayanak: {basedOnAgents}
Bu nihai kararı kısa bir paragrafla açıkla.";

        var orkestratorResponse = await orkestratorAsChatClient.GetResponseAsync(
            orkestratorPrompt,
            new ChatOptions { MaxOutputTokens = 450 });

        var finalVerdict = new FinalRiskVerdict
        {
            Level = finalLevel,
            Reason = orkestratorResponse.Text,
            BasedOnAgents = basedOnAgents,
        };

        return new RiskAnalysisResult(serviceName, kodRiskResult, testRiskResult, gecmisRiskResult, finalVerdict);
    }
}

public record RiskAnalysisResult(
    string ServiceName,
    RiskAssessment KodRiskAjani,
    RiskAssessment TestRiskAjani,
    RiskAssessment GecmisRiskAjani,
    FinalRiskVerdict NihaiKarar);
