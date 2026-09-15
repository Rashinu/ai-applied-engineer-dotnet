using MarketplaceRegulationAgent.Contracts;
using MarketplaceRegulationAgent.Domain;
using MarketplaceRegulationAgent.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.AI;
namespace MarketplaceRegulationAgent.ValidationWorker;

public class ProductSubmittedConsumer : IConsumer<ProductSubmitted>
{
    private readonly RegulationDbContext _db;
    private readonly MessageTenantAccessor _tenantAccessor;
    private readonly IProductEmbeddingGenerator _embeddingGenerator;

    private readonly  IChatClient _chatClient;

    public ProductSubmittedConsumer(RegulationDbContext db, MessageTenantAccessor tenantAccessor, IProductEmbeddingGenerator embeddingGenerator, IChatClient chatClient)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
    }

    public async Task Consume(ConsumeContext<ProductSubmitted> context)
    {
        // TODO: 1. _tenantAccessor.TenantId'yi mesajdaki TenantId ile doldur
        _tenantAccessor.TenantId = context.Message.TenantId;
        
        // TODO: 2. context.Message.ProductId ile ürünü DB'den bul (_db.Products)
        var  product = await _db.Products.FirstOrDefaultAsync(p => p.Id == context.Message.ProductId);
        if (product == null)
        {
            // Ürün bulunamazsa, bir hata fırlatabilir veya loglayabilirsiniz
            throw new Exception($"Product with ID {context.Message.ProductId} not found.");
        }
        product.Embedding = await _embeddingGenerator.GenerateEmbeddingAsync($"Product {product.Name}: {product.Category}");
        

        var similarRejectedProducts = await _db.Products
            .Where(p => p.Status == ProductStatus.Rejected && p.Embedding != null)
            .OrderBy(p=>p.Embedding!.L2Distance(product.Embedding!))
            .Take(5)
            .Select(p => p.Name)
            .ToListAsync(); // Bu örnek, benzerlik kontrolü için basit bir sıralama kullanır. Gerçek uygulamada daha karmaşık bir benzerlik algoritması kullanılabilir.
            
        var prompt = ProductValidationPromptBuilder.Build(product.Name, product.Category, product.Price, similarRejectedProducts);

        var aiResponse = await _chatClient.GetResponseAsync<ProductValidationResult>(prompt);
        var result = aiResponse.Result;
var validation = new ProductValidation
{
    ProductId = context.Message.ProductId,
    TenantId = context.Message.TenantId,
    IsCompliant = result.IsCompliant,
    CategoryMismatch = result.CategoryMismatch,
    PriceAnomalyScore = result.PriceAnomalyScore,
    Reasoning = result.Reasoning,
    Violations = result.Violations,
    ValidatedAt = DateTime.UtcNow
};

product.Status = result.IsCompliant ? ProductStatus.Approved : ProductStatus.Rejected;

        // TODO: 6. _db.SaveChangesAsync() çağır
        _db.ProductValidations.Add(validation);
        await _db.SaveChangesAsync();
        // TODO: 7. context.Publish ile bir ProductValidated mesajı yayınla
        await context.Publish(new ProductValidated(
            context.Message.ProductId,
            context.Message.TenantId,
            result.IsCompliant,
            result.CategoryMismatch,
            result.PriceAnomalyScore,
            result.Reasoning,
            result.Violations
        ));
    }
}
