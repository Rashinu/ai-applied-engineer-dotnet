namespace MarketplaceRegulationAgent.Contracts;

public class ProductValidationResult
{
   public bool IsCompliant { get; set; }

   public List<string> Violations { get; set; } = [];

   public bool CategoryMismatch { get; set; }

    public double PriceAnomalyScore { get; set; }

    public string Reasoning { get; set; } = string.Empty;
}