using MarketplaceRegulationAgent.Infrastructure;

namespace MarketplaceRegulationAgent.ValidationWorker;

/// <summary>
/// ICurrentTenantAccessor'ın Worker tarafındaki implementasyonu. Api'deki
/// HttpTenantAccessor'dan farkı: burada "istek" yok, "mesaj" var — bu yüzden
/// TenantId dışarıdan set edilebilir (mutable). Consumer, mesajı işlemeye
/// başlarken (RegulationDbContext'e hiç dokunmadan ÖNCE) bunu doldurur;
/// RegulationDbContext daha sonra ilk kullanıldığında bu değeri okur.
/// Bu sınıf MassTransit'in her mesaj için oluşturduğu DI scope'una "Scoped"
/// olarak kaydedilir, yani her mesaj kendi TenantId'sini taşır, karışmaz.
/// </summary>
public class MessageTenantAccessor : ICurrentTenantAccessor
{
    public Guid TenantId { get; set; }
}
