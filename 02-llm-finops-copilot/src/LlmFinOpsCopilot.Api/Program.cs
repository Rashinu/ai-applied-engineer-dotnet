using LlmFinOpsCopilot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LlmDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres"));

var app = builder.Build();


app.MapGet("/insights", async (LlmDbContext db) =>
{
    //Buraya:Analyst/Program.cs'deki kodu kopyala
    var latestTimestamp = await db.LlmCallLogs.MaxAsync(l => l.Timestamp);
    var sevenDaysAgo = latestTimestamp.AddDays(-7);
    var fourteenDaysAgo = latestTimestamp.AddDays(-14);      
    var recentCost = await db.LlmCallLogs
        .Where(l => l.Timestamp >= sevenDaysAgo && l.Timestamp <= latestTimestamp)
        .SumAsync(l => l.Cost);
    var previousCost = await db.LlmCallLogs
        .Where(l => l.Timestamp >= fourteenDaysAgo && l.Timestamp < sevenDaysAgo)  
        .SumAsync(l => l.Cost);
    var trendPercent = previousCost == 0 ? 0 : ((recentCost - previousCost) / previousCost) * 100;
    var anomalyCount = await db.AnomalyWindows.CountAsync();
    var mostFrequentPrompt = await db.LlmCallLogs
        .GroupBy(l => l.Prompt)
        .Select(g => new { Prompt = g.Key, Count = g.Count() })
        .OrderByDescending(g => g.Count)
        .FirstOrDefaultAsync();

    IChatClient chatClient = new OllamaApiClient("http://localhost:11435", "llama3.2:3b");
var prompt = $"""
Sen bir LLM maliyet analisti asistanısın. Aşağıdaki üç kanıttan SADECE BİRİNİ seçip,
ona dayanan tek bir öneri üret.

Kanıt 1 (Trend): Geçen hafta maliyet, önceki haftaya göre %{trendPercent:F2} değişti.
Kanıt 2 (Anomaly): Geçmiş veride {anomalyCount} adet maliyet sıçraması tespit edildi.
Kanıt 3 (Cluster): "{mostFrequentPrompt?.Prompt}" prompt'u {mostFrequentPrompt?.Count} kez tekrarlandı.

Yanıtında MUTLAKA şu üç alanın hepsini doldur, hiçbirini boş bırakma:
- recommendation: seçtiğin kanıda dayanan, 1-2 cümlelik somut bir öneri metni.
- evidenceType: seçtiğin kanıtın adı, tam olarak "Trend", "Anomaly" ya da "Cluster" olmalı.
- evidenceValue: seçtiğin kanıtın yukarıdaki TAM sayısal değeri (uydurma bir sayı yazma).
""";
var response = await chatClient.GetResponseAsync<AnalystRecommendation>(prompt);
Console.WriteLine("--- HAM YANIT ---");
Console.WriteLine(response.Text);
Console.WriteLine("--- /HAM YANIT ---");
var result = response.Result;

bool isGrounded = result.EvidenceType switch
{
    "Trend" => Math.Abs(result.EvidenceValue - (double)trendPercent) < 0.01,
    "Anomaly" => Math.Abs(result.EvidenceValue - (double)anomalyCount) < 0.01,
    "Cluster" => Math.Abs(result.EvidenceValue - (double)(mostFrequentPrompt?.Count ?? 0)) < 0.01,
    _ => false 
};
    //kanıt sorguları + IchatClient ile prompt oluşturma ve yanıt alma işlemleri + grounding ve recommendation oluşturma

    return Results.Ok(new
    {
        Trend = trendPercent,
        AnomalyCount = anomalyCount,
        MostFrequentPrompt = mostFrequentPrompt?.Prompt,
        MostFrequentPromptCount = mostFrequentPrompt?.Count,
        Recommendation = result.Recommendation,
        EvidenceType = result.EvidenceType,
        EvidenceValue = result.EvidenceValue,
        IsGrounded = isGrounded       
    });
});

app.Run();