using MarketplaceRegulationAgent.Api;
using MarketplaceRegulationAgent.Contracts;
using MarketplaceRegulationAgent.Domain;
using MarketplaceRegulationAgent.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire: OpenTelemetry, health checks, service discovery vs.
builder.AddServiceDefaults();

// Bu HTTP isteğinin hangi tenant'a ait olduğunu (X-Tenant-Id header'ından) okur.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantAccessor, HttpTenantAccessor>();

// Aspire'ın sağladığı "regulationdb" bağlantısını kullanarak RegulationDbContext'i kaydet.
// (AddNpgsqlDbContext DEĞİL — o, DbContext'i "pooled" kaydeder ve pooled context'ler
// Scoped bağımlılık (ICurrentTenantAccessor) kabul etmez; her istekte doğru tenant'ı
// almamız gerektiği için pooling'i devre dışı bırakıp standart AddDbContext kullanıyoruz.)
builder.Services.AddDbContext<RegulationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("regulationdb")));

// RabbitMQ + MassTransit — Aspire'ın "rabbitmq" bağlantı string'ini kullanır.
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));
    });
});



var app = builder.Build();

// Uygulama başlarken bekleyen migration'ları otomatik uygula (dev/demo ortamı için
// pratik bir yöntem — production'da genelde ayrı bir migration adımı tercih edilir).
// ICurrentTenantAccessor'ı DI'dan almıyoruz çünkü burada bir HTTP isteği yok
// (HttpTenantAccessor patlar); migration'lar zaten tenant'tan bağımsız şema
// işlemleri, o yüzden sabit/boş bir tenant ile geçici bir context yeterli.
using (var migrationScope = app.Services.CreateScope())
{
    var configuration = migrationScope.ServiceProvider.GetRequiredService<IConfiguration>();
    var optionsBuilder = new DbContextOptionsBuilder<RegulationDbContext>();
    optionsBuilder.UseNpgsql(configuration.GetConnectionString("regulationdb"));
    using var migrationDb = new RegulationDbContext(optionsBuilder.Options, new StartupTenantAccessor());
    await migrationDb.Database.MigrateAsync();
}

app.MapPost("/products", async (
    CreateProductRequest request,
    RegulationDbContext db,
    ICurrentTenantAccessor tenantAccessor,
    IPublishEndpoint publishEndpoint) =>
{
    var product = new Product
    {
        Id = Guid.NewGuid(),
        TenantId = tenantAccessor.TenantId,
        Name = request.Name,
        Category = request.Category,
        Price = request.Price,
        Quantity = request.Quantity,
        Status = ProductStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
    // TODO: 1. Yeni bir Product nesnesi oluştur:
    //   - Id = Guid.NewGuid()
    //   - TenantId = tenantAccessor.TenantId
    //   - Name, Category, Price, Quantity = request'ten
    //   - Status = ProductStatus.Pending
    //   - CreatedAt = DateTime.UtcNow

    // TODO: 2. db.Products.Add(...) ile ekle
    db.Products.Add(product);

    // TODO: 3. await db.SaveChangesAsync();
    await db.SaveChangesAsync();

    // TODO: 4. await publishEndpoint.Publish(new ProductSubmitted(product.Id, product.TenantId, DateTime.UtcNow));
await publishEndpoint.Publish(new ProductSubmitted(product.Id, product.TenantId, DateTime.UtcNow));
    // TODO: 5. return Results.Accepted($"/products/{product.Id}", new { product.Id });
    return Results.Accepted($"/products/{product.Id}", new { product.Id });
});

app.MapDefaultEndpoints();

app.Run();

record CreateProductRequest(string Name, string Category, decimal Price, int Quantity);
