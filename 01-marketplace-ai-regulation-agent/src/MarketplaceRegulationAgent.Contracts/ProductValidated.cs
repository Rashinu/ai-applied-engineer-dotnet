namespace MarketplaceRegulationAgent.Contracts;

public record ProductValidated(Guid ProductId, Guid TenantId, bool IsCompliant,
    bool CategoryMismatch, double PriceAnomalyScore, string Reasoning, List<string> Violations);
