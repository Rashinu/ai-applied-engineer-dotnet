var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithImage("pgvector/pgvector","pg16").WithVolume("regulationdb-data", "/var/lib/postgresql/data");
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithImage("rabbitmq","3.12-management").WithVolume("rabbitmq-data", "/var/lib/rabbitmq");
var regulationDb = postgres.AddDatabase("regulationdb");

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();

var chatModel = ollama.AddModel("chat", "llama3.2:3b");
var embeddingModel = ollama.AddModel("embeddings", "nomic-embed-text");

var api = builder.AddProject<Projects.MarketplaceRegulationAgent_Api>("api")
    .WithReference(regulationDb)
    .WithReference(rabbitmq)
    .WaitFor(regulationDb)
    .WaitFor(rabbitmq);

var validationWorker = builder.AddProject<Projects.MarketplaceRegulationAgent_ValidationWorker>("validation-worker")
    .WithReference(regulationDb)
    .WithReference(rabbitmq)
    .WithReference(chatModel)
    .WithReference(embeddingModel)
    .WaitFor(regulationDb)
    .WaitFor(rabbitmq)
    .WaitFor(chatModel)
    .WaitFor(embeddingModel);

builder.Build().Run();
