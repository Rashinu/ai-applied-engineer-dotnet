using Pgvector;

namespace MarketplaceRegulationAgent.Infrastructure;

public class FakeEmbeddingGenerator : IProductEmbeddingGenerator
{
    private const int EmbeddingDimension = 768;

    public Task<Vector> GenerateEmbeddingAsync(string productDescription)
    {
        int seed = productDescription.GetHashCode();
        var random = new Random(seed);

        var embedding = new float[EmbeddingDimension];
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1);
        }

        return Task.FromResult(new Vector(embedding));
    }
}
