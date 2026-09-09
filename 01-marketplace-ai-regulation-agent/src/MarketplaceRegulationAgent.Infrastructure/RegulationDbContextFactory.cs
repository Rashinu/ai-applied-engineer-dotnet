using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketplaceRegulationAgent.Infrastructure;

/// <summary>
/// "dotnet ef migrations" gibi tasarım-zamanı (design-time) araçları için kullanılır.
/// Gerçek uygulama çalışırken bu sınıf hiç devreye girmez — sadece migration
/// oluştururken EF'in "hangi tenant?" sorusuna cevap veremediği için, burada
/// sahte/boş bir Guid ile bir örnek üretiyoruz. Amaç sadece şemayı (tablo/kolon
/// yapısını) görebilmek, gerçek veriyle bir ilgisi yok.
/// </summary>
public class RegulationDbContextFactory : IDesignTimeDbContextFactory<RegulationDbContext>
{
    public RegulationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RegulationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=regulationdb;Username=postgres;Password=postgres");

        return new RegulationDbContext(optionsBuilder.Options, Guid.Empty);
    }
}
