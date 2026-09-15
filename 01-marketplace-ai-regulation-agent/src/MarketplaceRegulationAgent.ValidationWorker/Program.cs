using MarketplaceRegulationAgent.Infrastructure;
using MarketplaceRegulationAgent.ValidationWorker;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddScoped<MessageTenantAccessor>();
builder.Services.AddScoped<ICurrentTenantAccessor>(sp => sp.GetRequiredService<MessageTenantAccessor>());
// Ollama'nın "embeddings" bağlantısına (Aspire'ın enjekte ettiği) bağlanıp
// IEmbeddingGenerator<string, Embedding<float>>'i DI'a kaydeder.
builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator();
builder.Services.AddSingleton<IProductEmbeddingGenerator, RealEmbeddingGenerator>();

// Ollama'nın "chat" bağlantısına bağlanıp IChatClient'ı DI'a kaydeder —
// GetResponseAsync<T> ile typed structured output için kullanılacak.
builder.AddOllamaApiClient("chat").AddChatClient();

builder.Services.AddDbContext<RegulationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("regulationdb"), o => o.UseVector()));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductSubmittedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
