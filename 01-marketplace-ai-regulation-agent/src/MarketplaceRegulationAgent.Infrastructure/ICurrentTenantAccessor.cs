namespace MarketplaceRegulationAgent.Infrastructure;

/// <summary>
/// "Şu an hangi tenant için çalışıyoruz" bilgisini RegulationDbContext'e verir.
/// Api'de bir HTTP isteğinin X-Tenant-Id header'ından, Worker'da ise işlenmekte
/// olan mesajın TenantId'sinden doldurulur — DbContext'in kendisi bunu bilmez,
/// sadece bu arayüz üzerinden sorar.
/// </summary>
public interface ICurrentTenantAccessor
{
    Guid TenantId { get; }
}
