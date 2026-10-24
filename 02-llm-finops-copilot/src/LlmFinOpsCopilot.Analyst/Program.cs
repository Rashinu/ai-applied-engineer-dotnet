using LlmFinOpsCopilot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;

var optionsBuilder = new DbContextOptionsBuilder<LlmDbContext>()
    .UseNpgsql("Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres")
    .Options;
using var db = new LlmDbContext(optionsBuilder);

// "Şimdi" olarak verideki en son zamanı kullanıyoruz (sentetik veri üretim anında
// bitiyor, gerçek DateTime.UtcNow ile karşılaştırırsak boş aralıklar çıkabilir)
var latestTimestamp = await db.LlmCallLogs.MaxAsync(l => l.Timestamp);

// Kanıt 1: Trend — son 7 gün vs önceki 7 gün ortalama maliyet, yüzde değişim
var sevenDaysAgo = latestTimestamp.AddDays(-7);
var fourteenDaysAgo = latestTimestamp.AddDays(-14);
var recentCost = await db.LlmCallLogs
    .Where(l => l.Timestamp >= sevenDaysAgo && l.Timestamp <= latestTimestamp)
    .SumAsync(l => l.Cost);
var previousCost = await db.LlmCallLogs
    .Where(l => l.Timestamp >= fourteenDaysAgo && l.Timestamp < sevenDaysAgo)  
    .SumAsync(l => l.Cost);

var trendPercent = previousCost == 0 ? 0 : ((recentCost - previousCost) / previousCost) * 100;

// Kanıt 2: Anomali sayısı — AnomalyWindows tablosundaki toplam pencere sayısı

var anomalyCount = await db.AnomalyWindows.CountAsync();
// Kanıt 3: En çok tekrar eden prompt — GroupBy + Count + OrderByDescending
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



Console.WriteLine($"Öneri: {result.Recommendation}");
Console.WriteLine($"Dayandığı kanıt: {result.EvidenceType} = {result.EvidenceValue}");

// Grounding-check: LLM'in verdiği EvidenceValue, gerçekten hesapladığımız
// üç kanıttan birine (küçük bir tolerans payıyla) eşit mi?
bool isGrounded = result.EvidenceType switch
{
    "Trend" => Math.Abs(result.EvidenceValue - (double)trendPercent) < 0.5,
    "Anomaly" => Math.Abs(result.EvidenceValue - anomalyCount) < 0.5,
    "Cluster" => Math.Abs(result.EvidenceValue - (mostFrequentPrompt?.Count ?? 0)) < 0.5,
    _ => false
};

Console.WriteLine(isGrounded
    ? "GROUNDING-CHECK: GEÇTİ — öneri gerçek kanıta dayanıyor."
    : "GROUNDING-CHECK: BAŞARISIZ — öneri kanıtsız/uydurma bir sayıya dayanıyor, REDDEDİLDİ.");


Console.WriteLine($"Trend: %{trendPercent:F2} (son 7 gün: {recentCost:F2}, önceki 7 gün: {previousCost:F2})");
Console.WriteLine($"Anomali sayısı: {anomalyCount}");
Console.WriteLine($"En sık tekrar eden prompt: \"{mostFrequentPrompt?.Prompt}\" ({mostFrequentPrompt?.Count} kez)");
