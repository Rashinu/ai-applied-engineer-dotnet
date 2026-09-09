namespace MarketplaceRegulationAgent.Contracts;

public record ProductSubmitted(Guid ProductId, Guid TenantId, DateTime SubmittedAt);
