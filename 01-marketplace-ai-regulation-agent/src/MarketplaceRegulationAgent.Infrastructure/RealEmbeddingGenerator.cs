using Microsoft.Extensions.AI;
using Pgvector;

namespace MarketplaceRegulationAgent.Infrastructure;

/// <summary>
/// IProductEmbeddingGenerator'ın gerçek implementasyonu — Ollama'ya (nomic-embed-text
/// modeli) HTTP üzerinden bağlanan Microsoft.Extensions.AI soyutlamasını kullanır.
/// FakeEmbeddingGenerator'ın (Gün 4) yerini alıyor; Consumer hiç değişmedi çünkü
/// ikisi de aynı IProductEmbeddingGenerator arayüzünü uyguluyor.
/// </summary>
public class RealEmbeddingGenerator : IProductEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public RealEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string productDescription)
    {
        var vector = await _generator.GenerateVectorAsync(productDescription);
        return new Vector(vector.ToArray());
    }
}
