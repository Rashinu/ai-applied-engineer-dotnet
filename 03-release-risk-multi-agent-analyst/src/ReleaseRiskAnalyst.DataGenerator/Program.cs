using ReleaseRiskAnalyst.Domain;
using Microsoft.EntityFrameworkCore;
using ReleaseRiskAnalyst.Infrastructure;

double CalculateReleaseRisk(double linesChanged, double reviewerCount, double failedCiRunCount, double testCoverage)
{
    // Normalize the inputs to a scale of 0 to 1
    double normalizedLinesChanged = Math.Min(linesChanged / 1000.0, 1.0); // Assuming 1000 lines changed is the maximum risk
    double normalizedReviewerCount = Math.Min(reviewerCount / 5.0, 1.0); // Assuming 5 reviewers is the maximum risk
    double normalizedFailedCiRunCount = Math.Min(failedCiRunCount / 10.0, 1.0); // Assuming 10 failed CI runs is the maximum risk
    double normalizedTestCoverage = Math.Max(0.0, Math.Min(testCoverage / 100.0, 1.0)); // Test coverage should be between 0 and 100%

    // Calculate risk factors based on the normalized inputs
    double riskFromLinesChanged = normalizedLinesChanged * 0.4; // Weight of lines changed
    double riskFromReviewerCount = (1 - normalizedReviewerCount) * 0.3; // Less reviewers increases risk
    double riskFromFailedCiRuns = normalizedFailedCiRunCount * 0.2; // More failed CI runs increases risk
    double riskFromTestCoverage = (1 - normalizedTestCoverage) * 0.1; // Lower test coverage increases risk

    // Combine the risk factors to calculate overall release risk
    double totalRisk = riskFromLinesChanged + riskFromReviewerCount + riskFromFailedCiRuns + riskFromTestCoverage;

    return totalRisk;
}

var releases = new List<ReleaseSignal>();
var incidents = new List<ServiceIncidentHistory>();

string[] services = ["payment-service", "checkout-service", "inventory-service", "auth-service"];
IncidentSeverity[] severities = [IncidentSeverity.Low, IncidentSeverity.Medium, IncidentSeverity.High, IncidentSeverity.Critical];

int releaseCount = 300; // Number of releases to generate
DateTime startDate = DateTime.UtcNow.AddDays(-180);

for (int i = 0; i < releaseCount; i++)
{
    var release = new ReleaseSignal
    {
        SignalId = Guid.NewGuid(),
        ServiceName = services[Random.Shared.Next(services.Length)],
        DeployedAt = startDate.AddHours(Random.Shared.Next(0, 180 * 24)),
        LinesChanged = Random.Shared.Next(0, 2000),
        CommitCount = Random.Shared.Next(1, 30),
        ReviewerCount = Random.Shared.Next(1, 6),
        TestCoverage = Random.Shared.NextDouble() * 100,
        FailedCiRunCount = Random.Shared.Next(0, 11)
    };
    releases.Add(release);

    double riskScore = CalculateReleaseRisk(
        linesChanged: release.LinesChanged,
        reviewerCount: release.ReviewerCount,
        failedCiRunCount: release.FailedCiRunCount,
        testCoverage: release.TestCoverage
    );

    if (Random.Shared.NextDouble() < riskScore)
    {
        var incident = new ServiceIncidentHistory
        {
            IncidentId = Guid.NewGuid(),
            ServiceName = release.ServiceName,
            OccurredAt = release.DeployedAt.AddDays(Random.Shared.Next(1, 5)),
            Severity = severities[Random.Shared.Next(severities.Length)]
        };
        incidents.Add(incident);
    }
}

Console.WriteLine($"Toplam {releases.Count} release, {incidents.Count} incident üretildi.");

var optionsBuilder = new DbContextOptionsBuilder<ReleaseRiskDbContext>()
    .UseNpgsql("Host=localhost;Port=5434;Database=releaseriskdb;Username=postgres;Password=postgres")
    .Options;

using var db = new ReleaseRiskDbContext(optionsBuilder);
await db.Database.EnsureCreatedAsync();
await db.ReleaseSignals.AddRangeAsync(releases);
await db.ServiceIncidentHistories.AddRangeAsync(incidents);
await db.SaveChangesAsync();


Console.WriteLine("Veriler veritabanına kaydedildi.");