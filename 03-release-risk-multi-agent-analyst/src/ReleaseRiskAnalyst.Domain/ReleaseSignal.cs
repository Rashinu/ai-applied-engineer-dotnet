namespace ReleaseRiskAnalyst.Domain;

public class ReleaseSignal
{
    public Guid SignalId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public int LinesChanged { get; set; }
    public int CommitCount { get; set; }
    public int ReviewerCount { get; set; }
    public double TestCoverage { get; set; }
    public int FailedCiRunCount { get; set; }
}
