public class FinalRiskVerdict
{
    public RiskAssessment.RiskLevel Level { get; set; }
    public string Reason { get; set; } = string.Empty;

    public string BasedOnAgents { get; set; } = string.Empty;
}