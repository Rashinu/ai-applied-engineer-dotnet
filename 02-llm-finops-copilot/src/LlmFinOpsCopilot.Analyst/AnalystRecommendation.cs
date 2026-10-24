public class AnalystRecommendation
{
    public string Recommendation { get; set; } = "";
    public string EvidenceType { get; set; } = "";   // "Trend", "Anomaly" ya da "Cluster"
    public double EvidenceValue { get; set; }
}
