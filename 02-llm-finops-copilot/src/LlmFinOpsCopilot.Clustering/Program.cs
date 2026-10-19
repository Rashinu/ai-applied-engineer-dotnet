using LlmFinOpsCopilot.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using Microsoft.ML;
using Microsoft.ML.Data;

var optionsBuilder = new DbContextOptionsBuilder<LlmDbContext>()
    .UseNpgsql("Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres")
    .Options;
using var db = new LlmDbContext(optionsBuilder);

var distinctPrompts = await db.LlmCallLogs.Select(l => l.Prompt).Distinct().ToListAsync();
Console.WriteLine($"Distinct prompts count: {distinctPrompts.Count}");

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator =
    new OllamaApiClient(new Uri("http://localhost:11435"), "nomic-embed-text");

var promptEmbeddings = new List<(string Prompt, float[] Vector)>();
foreach (var prompt in distinctPrompts)
{
    var embedding = await embeddingGenerator.GenerateVectorAsync(prompt);
    promptEmbeddings.Add((prompt, embedding.ToArray()));
}

Console.WriteLine($"Generated embeddings for {promptEmbeddings.Count} prompts.");
Console.WriteLine($"First embedding length: {promptEmbeddings[0].Vector.Length}");

var mlContext = new MLContext();

var mlInputs = promptEmbeddings
    .Select(p => new PromptVectorInput { Prompt = p.Prompt, Features = p.Vector })
    .ToList();

var mlData = mlContext.Data.LoadFromEnumerable(mlInputs);

var clusterPipeline = mlContext.Transforms.NormalizeMinMax("Features")
    .Append(mlContext.Clustering.Trainers.KMeans("Features", numberOfClusters: 10));


var clusterModel = clusterPipeline.Fit(mlData);
var clusteredData = clusterModel.Transform(mlData);

var clusteredResults = mlContext.Data.CreateEnumerable<ClusterPrediction>(clusteredData, reuseRowObject: false).ToList();

var groups = clusteredResults.GroupBy(r => r.Cluster).OrderBy(g => g.Key);
foreach (var group in groups)
{
    Console.WriteLine($"Küme {group.Key}: {group.Count()} prompt");
}

public class PromptVectorInput
{
    public string Prompt { get; set; } = string.Empty;
    [VectorType(768)]
    public float[] Features { get; set; } = Array.Empty<float>();
}

public class ClusterPrediction
{
    [ColumnName("PredictedLabel")]
    public uint Cluster { get; set; }

    public string Prompt { get; set; } = string.Empty;
}