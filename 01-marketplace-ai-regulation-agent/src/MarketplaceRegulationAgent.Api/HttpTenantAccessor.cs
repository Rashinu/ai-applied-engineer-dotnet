using MarketplaceRegulationAgent.Infrastructure;

namespace MarketplaceRegulationAgent.Api;

/// <summary>
/// ICurrentTenantAccessor'ın Api tarafındaki implementasyonu: gelen HTTP isteğinin
/// "X-Tenant-Id" header'ından tenant kimliğini okur. MVP basitleştirmesi — gerçek
/// kimlik doğrulama (JWT vs.) henüz yok, header'a doğrudan güveniyoruz.
/// </summary>
public class HttpTenantAccessor : ICurrentTenantAccessor
{
    public Guid TenantId { get; }

    public HttpTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(header) || !Guid.TryParse(header, out var tenantId))
        {
            throw new InvalidOperationException("X-Tenant-Id header eksik veya geçersiz.");
        }

        TenantId = tenantId;
    }
}

/// <summary>
/// Migration gibi HTTP isteği olmadan çalışan startup adımları için sabit/boş
/// tenant sağlayan yardımcı sınıf — RegulationDbContextFactory'deki design-time
/// karşılığıyla aynı mantık.
/// </summary>
public class StartupTenantAccessor : ICurrentTenantAccessor
{
    public Guid TenantId => Guid.Empty;
}
