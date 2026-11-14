public class CodeRiskAssessment
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }
    public RiskLevel Level { get; set; }
    public string Reason { get; set; } = string.Empty;
}