using MarketplaceRegulationAgent.Infrastructure;
using MarketplaceRegulationAgent.ValidationWorker;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddScoped<MessageTenantAccessor>();
builder.Services.AddScoped<ICurrentTenantAccessor>(sp => sp.GetRequiredService<MessageTenantAccessor>());

builder.Services.AddDbContext<RegulationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("regulationdb")));

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
