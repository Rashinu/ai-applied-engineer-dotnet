namespace ReleaseRiskAnalyst.Domain;

public class ServiceIncidentHistory
{
    public Guid IncidentId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public IncidentSeverity Severity { get; set; }
}

public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical
}
