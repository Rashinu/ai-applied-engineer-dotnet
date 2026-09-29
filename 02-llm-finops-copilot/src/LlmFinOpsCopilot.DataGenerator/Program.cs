using LlmFinOpsCopilotDomain;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using LlmFinOpsCopilot.Infrastructure;

var logs = new List<LlmCallLog>();
string [] providers = ["OpenAI", "Anthropic", "Ollama"];
var baseRate = 185; // Base number of calls per hour


var providerModels = new Dictionary<string, string[]>
{
    { "OpenAI", new[] { "gpt-3.5-turbo", "gpt-4" } },
    { "Anthropic", new[] { "claude-v1", "claude-v2" } },
    { "Ollama", new[] { "ollama-model-1", "ollama-model-2" } }
};
DateTime startDate = DateTime.UtcNow.AddDays(-90); // Start date 30 days ago
DateTime endDate = DateTime.UtcNow; // End date is now

double HourMultiplier (int hourOfDay)
{
    return (hourOfDay >= 9 && hourOfDay <= 17) ? 1.5 : 1.0; // Higher multiplier during business hours (9 AM to 5 PM)
}

double DayMultiplier (DayOfWeek dayOfWeek)
{
    return (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) ? 0.5 : 1.0; // Lower multiplier on weekends
}

var anomalyWindows = new List<(DateTime start, DateTime end)>();
int anomalyCount = Random.Shared.Next(5, 11); // Randomly choose between 3 to 5 anomaly windows

for (int i = 0; i < anomalyCount; i++)
{
    DateTime anomalyStart = startDate.AddDays(Random.Shared.Next(0, 90)).AddHours(Random.Shared.Next(0, 24));
    DateTime anomalyEnd = anomalyStart.AddHours(Random.Shared.Next(1, 5)); // Anomalies last between 1 to 5 hours
    anomalyWindows.Add((anomalyStart, anomalyEnd));
}

for (DateTime date = startDate; date <= endDate; date = date.AddHours(1))
{
double multiplier = HourMultiplier(date.Hour) * DayMultiplier(date.DayOfWeek);
int countThisHour = (int)(baseRate*multiplier);
bool isAnomaly = false;
foreach (var window in anomalyWindows)
{
    if (date >= window.start && date <= window.end)
    {
        isAnomaly = true;
        break;
    }
}
if (isAnomaly)
{
    countThisHour *= Random.Shared.Next(5, 11); // Triple the count during anomaly windows
}
for (int i = 0; i < countThisHour; i++)
{
var providerNames = providerModels.Keys.ToArray();
string selectedProvider = providerNames[Random.Shared.Next(providerNames.Length)];
string[] models = providerModels[selectedProvider];
string selectedModel = models[Random.Shared.Next(models.Length)];
int promptTokens = Random.Shared.Next(10, 1000);
int completionTokens = Random.Shared.Next(10, 1000);
decimal pricePerToken = 0.0001m; // Example price per token

var log = new LlmCallLog
{
    Id = Guid.NewGuid(),
    UserId = $"user_{Random.Shared.Next(1, 100)}",
    Prompt = $"This is a sample prompt {Random.Shared.Next(1, 100)}",
    Timestamp = date,
    Provider = selectedProvider,
    Model = selectedModel,
    PromptTokenCount = promptTokens,
    CompletionTokenCount = completionTokens,
    LatencyMs = Random.Shared.NextDouble() * 1000, // Random latency between 0 and 1000 ms
    CacheHit = Random.Shared.Next(0, 2) == 1, // Randomly true or false
    Team = $"team_{Random.Shared.Next(1, 10)}",
    Cost = (promptTokens + completionTokens) * pricePerToken
};

logs.Add(log);
}
}
var options = new DbContextOptionsBuilder<LlmDbContext>()
    .UseNpgsql("Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres")
    .Options;
using var context = new LlmDbContext(options);
await context.LlmCallLogs.AddRangeAsync(logs);
await context.SaveChangesAsync();

Console.WriteLine($"Toplam {logs.Count} log üretildi.");
Console.WriteLine("Veritabanına yazıldı.");