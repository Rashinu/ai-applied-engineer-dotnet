using MarketplaceRegulationAgent.Contracts;
using MarketplaceRegulationAgent.Domain;
using MarketplaceRegulationAgent.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace MarketplaceRegulationAgent.ValidationWorker;

public class ProductSubmittedConsumer : IConsumer<ProductSubmitted>
{
    private readonly RegulationDbContext _db;
    private readonly MessageTenantAccessor _tenantAccessor;
    private readonly IProductEmbeddingGenerator _embeddingGenerator;

    public ProductSubmittedConsumer(RegulationDbContext db, MessageTenantAccessor tenantAccessor, IProductEmbeddingGenerator embeddingGenerator)
    {
        _db = db;
        _tenantAccessor = tenantAccessor;
        _embeddingGenerator = embeddingGenerator;
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
            
    var reasoningText = similarRejectedProducts.Count > 0
    ? $"Sahte doğrulama sonucu: her zaman uyumlu. Benzer reddedilmiş ürünler: {string.Join(", ", similarRejectedProducts)}"
    : "Sahte doğrulama sonucu: her zaman uyumlu. Benzer reddedilmiş ürün bulunamadı.";

        // TODO: 3. Şimdilik SAHTE bir sonuç üret (her zaman IsCompliant = true)
        var isCompliant = true;
        // TODO: 4. Yeni bir ProductValidation nesnesi oluştur, _db.ProductValidations'a ekle
        var validaiton = new ProductValidation
        {
            ProductId = context.Message.ProductId,
            TenantId = context.Message.TenantId,
            IsCompliant = isCompliant,
            CategoryMismatch = false,
            PriceAnomalyScore = 0.0,
            Reasoning = reasoningText,
            ValidatedAt = DateTime.UtcNow
        };
        // TODO: 5. product.Status'ü ProductStatus.Approved yap
        product.Status = ProductStatus.Approved;
        // TODO: 6. _db.SaveChangesAsync() çağır
        _db.ProductValidations.Add(validaiton);
        await _db.SaveChangesAsync();
        // TODO: 7. context.Publish ile bir ProductValidated mesajı yayınla
        await context.Publish(new ProductValidated(
            context.Message.ProductId,
            context.Message.TenantId,
            isCompliant,
            false,
            0.0,
            reasoningText,
            new List<string>()
        ));
    }
}
