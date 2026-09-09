namespace MarketplaceRegulationAgent.Domain;

    public class ProductValidation
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        
        public Guid TenantId { get; set; }
        
        public bool IsCompliant { get; set; }

        public bool CategoryMismatch { get; set; }

        public double PriceAnomalyScore { get; set; }

        public string Reasoning { get; set; } = string.Empty;

        public List<string> Violations { get; set; } = new List<string>();
        public DateTime ValidatedAt { get; set; }
    }