using Pgvector;

namespace MarketplaceRegulationAgent.Infrastructure;

public interface IProductEmbeddingGenerator
{
    Task<Vector> GenerateEmbeddingAsync(string productDescription);
}
