# İlerleme Günlüğü — Marketplace AI Regulation Agent

Kural: Bir günün **Durum**'u ✅ Review edildi olmadan bir sonraki güne geçilmez.
"Yapılanlar / Doğrulama" kısmını sen doldurursun, "Review notları" kısmını ben.

**Tempo notu**: Buradaki "Gün N" etiketleri takvim günü değil — kişisel haftalık
plandaki sabit .NET kod bloklarından (Salı 19:30-21:00, Cumartesi 11:30-13:30,
Pazar 19:30-21:00 — haftada ~5 saat garanti coding) her birine karşılık geliyor.
7 "Gün"lük bu proje planı, bu tempoyla gerçekte ~2-3 haftaya yayılacak; bu
beklenen ve istenen bir şey, amaç hız değil düzenli ilerleme.

| Gün | Tarih | Hafta günü / blok |
|-----|-------|--------------------|
| 1 | 2026-09-06 | Pazar 19:30-21:00 |
| 2 | 2026-09-09 | Çarşamba (plan dışı ek oturum — normalde İş Yeri Projesi bloğu) |
| 3 | 2026-09-09 | Çarşamba (Gün 2 ile aynı oturumda devam edildi) |
| 4 | 2026-09-13 | Pazar 19:30-21:00 |
| 5 | 2026-09-13 | Pazar (Gün 4 ile aynı oturumda devam edildi) |
| 6 | 2026-09-15 | Salı (plan dışı ek oturum) |
| 7 | 2026-09-15 | Salı (Gün 6 ile aynı oturumda devam edildi) |

---

## Mimari (referans — tüm günler boyunca sabit)

```
Api (ASP.NET Core minimal API)
  -> POST /products  → DB'ye kaydet (PendingValidation) → RabbitMQ'ya ProductSubmitted yayınla
  -> GET  /products/{id}/validation → sonucu sorgula

RabbitMQ (MassTransit üzerinden)

ValidationWorker (Worker Service, MassTransit consumer)
  -> ProductSubmittedConsumer:
       1. Product + Tenant'ı yükle
       2. pgvector: geçmişte reddedilen benzer ürünleri ara
       3. Prompt oluştur (ürün + policy kuralları + benzer reddedilenler)
       4. IChatClient.GetResponseAsync<ProductValidationResult>(prompt)  // Ollama, typed JSON output
       5. Sonucu Postgres'e yaz, Product.Status güncelle
       6. ProductValidated event'i yayınla

Postgres + pgvector   (Aspire container, EF Core + Pgvector.EntityFrameworkCore)
Ollama                (Aspire container: llama3.1:8b chat + nomic-embed-text embedding)
Aspire AppHost        (hepsini orkestre eder, dashboard'da görünür)
```

Hedef proje yapısı:

```
01-marketplace-ai-regulation-agent/
├── MarketplaceRegulationAgent.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json                        # sdk: 10.0.400
├── src/
│   ├── MarketplaceRegulationAgent.AppHost/
│   ├── MarketplaceRegulationAgent.ServiceDefaults/
│   ├── MarketplaceRegulationAgent.Contracts/      # message + AI result şekilleri
│   ├── MarketplaceRegulationAgent.Domain/          # Tenant, Product, ProductValidation
│   ├── MarketplaceRegulationAgent.Infrastructure/  # EF Core, RegulationDbContext, migrations
│   ├── MarketplaceRegulationAgent.Api/
│   └── MarketplaceRegulationAgent.ValidationWorker/
└── tests/
    ├── MarketplaceRegulationAgent.Domain.Tests/
    ├── MarketplaceRegulationAgent.Api.Tests/
    └── MarketplaceRegulationAgent.Worker.Tests/
```

Sabit kararlar:
- .NET 10 (yerelde 10.0.400 kurulu), Aspire ile orkestrasyon.
- Ollama Docker container olarak Aspire'dan yönetilir (`llama3.1:8b` — `llama3.3` 70B
  yerel demo için ağır; küçük modelle başla, istersen sonra büyüt).
- MassTransit + RabbitMQ (Aspire RabbitMQ container).
- PostgreSQL + pgvector: EF Core + `Pgvector`/`Pgvector.EntityFrameworkCore` paketleri
  (stabil, kanıtlanmış); `Microsoft.Extensions.VectorData` soyutlaması opsiyonel —
  paket adı ekosistemde değişken, önce doğrulamadan bağlanma.
- Multi-tenancy: MVP'de shared-schema + `TenantId` kolonu + EF Core global query filter
  (Api'de `X-Tenant-Id` header; Worker'da mesajdaki TenantId, filtre bypass edilip
  explicit `Where` ile — global filter + arka plan worker karışırsa tenant sızıntısı
  riski var, bu yüzden Worker'da bilinçli farklı davranılıyor).

### Bilinen riskler (ilgili günde doğrulanacak)

1. `Aspire.Hosting.Ollama` paketi .NET 10 + güncel Aspire ile var mı / olgun mu —
   yoksa `AddContainer("ollama", "ollama/ollama")` ile manuel kurulum. (Gün 5)
2. pgvector için `Microsoft.Extensions.VectorData` altında paket adı ekosistemde
   değişiyor — `Pgvector`/`Pgvector.EntityFrameworkCore` daha stabil, önce onunla başla. (Gün 4)
3. Ollama'nın OpenAI-uyumlu endpoint'i ile `response_format: json_schema` /
   `GetResponseAsync<T>` typed output kalitesi modelden modele değişir. (Gün 6)
4. `Microsoft.Extensions.AI` API yüzeyi hâlâ değişebiliyor — implementasyon anında
   kurulu paket versiyonuna göre metod adını doğrula. (Gün 6)
5. Embedding modeli (`nomic-embed-text`) `llama3.1:8b`'den ayrı, ikisi de Ollama'ya
   pull edilmeli. (Gün 5)

---

## Gün 1 — İskelet ve Aspire orkestrasyonu

- **Yap**: `global.json`, `Directory.Build.props`, `Directory.Packages.props` oluştur.
  `AppHost` ve `ServiceDefaults` projelerini `dotnet new aspire-apphost` /
  `dotnet new aspire-servicedefaults` ile oluştur. AppHost'a sadece Postgres
  (pgvector image'ı ile) ve RabbitMQ resource'larını ekle — Ollama'yı henüz ekleme.
- **Neden**: Aspire'ın nasıl çalıştığını (dashboard, resource wiring, connection
  string enjeksiyonu) tek başına anlamadan üstüne AI/messaging katmanı koymak
  kafa karıştırır.
- **Doğrulama**: `dotnet run` AppHost'tan çalıştığında dashboard açılıyor,
  Postgres ve RabbitMQ container'ları yeşil görünüyor, RabbitMQ management UI'a
  tıklayıp girebiliyorsun.
- **Claude'a gel**: AppHost.cs dosyanı göster, resource wiring doğru mu bak.

**Durum**: ✅ Review edildi (2026-09-06)
**Yapılanlar**:
- `global.json` (SDK 10.0.400, rollForward: latestFeature), `Directory.Build.props`
  (net10.0, nullable/implicit usings enable), `Directory.Packages.props` (Central
  Package Management) oluşturuldu.
- Aspire proje şablonları kurulmadığı için önce `dotnet new install Aspire.ProjectTemplates`
  çalıştırıldı.
- `dotnet new aspire-servicedefaults -o src/MarketplaceRegulationAgent.ServiceDefaults`
  ve `dotnet new aspire-apphost -o src/MarketplaceRegulationAgent.AppHost` ile
  projeler oluşturuldu.
- `dotnet new sln --format slnx -n MarketplaceRegulationAgent` ile çözüm dosyası
  oluşturuldu, her iki proje `dotnet sln add` ile eklendi.
- AppHost projesine `Aspire.Hosting.PostgreSQL` ve `Aspire.Hosting.RabbitMQ`
  paketleri eklendi (CPM sayesinde versiyonlar otomatik `Directory.Packages.props`'a
  yazıldı).
- `AppHost.cs`:
  ```csharp
  var builder = DistributedApplication.CreateBuilder(args);

  var postgres = builder.AddPostgres("postgres")
      .WithImage("pgvector/pgvector", "pg16")
      .WithVolume("regulationdb-data", "/var/lib/postgresql/data");

  var rabbitmq = builder.AddRabbitMQ("rabbitmq")
      .WithImage("rabbitmq", "3.12-management")
      .WithVolume("rabbitmq-data", "/var/lib/rabbitmq");

  var regulationDb = postgres.AddDatabase("regulationdb");

  builder.Build().Run();
  ```
- Karşılaşılan ve çözülen hatalar (ileride aynı hataya düşülürse hızlı referans):
  1. `dotnet new` komutları JSON parse hatası veriyordu → `global.json` boş
     dosyaydı, geçersiz JSON'du. İçerik doldurulunca düzeldi.
  2. `NU1008` restore hatası → CPM açıkken `.csproj`'da `PackageReference`
     üzerinde doğrudan `Version` yazılamıyor; ServiceDefaults şablonunun
     eklediği versiyonlar `Directory.Packages.props`'a `PackageVersion` olarak
     taşındı, `.csproj`'dan `Version` attribute'ları kaldırıldı.
  3. `dotnet add package` / `dotnet restore` "içinde proje bulunamadı" hatası →
     komutlar kök klasörden değil, ilgili proje klasöründen çalıştırılmalı.
  4. Aspire dashboard'da `postgres` "Runtime Unhealthy" → Docker Desktop
     kapalıydı, açılınca düzeldi.
  5. `postgres.AddDatabase("regulationdb", "postgres")` yazılmıştı — ikinci
     parametre gerçek veritabanı adını override ediyor, yanlışlıkla `postgres`
     (varsayılan db) kullanılıyordu. `AddDatabase("regulationdb")` olarak
     düzeltildi (tek parametre, hem resource adı hem db adı olarak kullanılıyor).
- Bilinen risk #2 (pgvector paket adı) bu günde gündeme gelmedi çünkü henüz
  EF Core/Pgvector.EntityFrameworkCore paketi eklenmedi — bu Gün 4'e ertelendi,
  şimdilik sadece doğru Docker image'ı (`pgvector/pgvector:pg16`) seçildi.
**Doğrulama sonucu**: `dotnet run` ile AppHost çalıştırıldı, Claude tarafından
Chrome DevTools ile dashboard'a girilip ekran görüntüsüyle doğrulandı: 3 resource
da `Running` (yeşil) — `postgres` (image: `pgvector/pgvector:pg16`), `regulationdb`
(postgres altında nested database), `rabbitmq` (image: `rabbitmq:3.12-management`).
**Review notları**: Kod doğru ve çalışır durumda. Küçük kozmetik not: RabbitMQ
management UI'ı manuel image (`3.12-management` tag) ile aktif edildiği için
Aspire dashboard'ın "URL'ler" sütununda HTTP linki olarak listelenmiyor (sadece
AMQP tcp portu görünüyor) — fonksiyonel olarak çalışıyor (kullanıcı elle
`localhost:15672`'ye giderek doğruladı), ama `.WithManagementPlugin()` extension
metodu kullanılsaydı Aspire bu portu otomatik tanıyıp tıklanabilir link
üretecekti. Şimdilik bilinçli olarak bu haliyle bırakıldı, ileride istenirse
düzeltilebilir (kozmetik, bloklayıcı değil).

---

## Gün 2 — Domain modeli ve EF Core (AI/messaging olmadan)

- **Yap**: `Domain` projesinde `Tenant`, `Product`, `ProductValidation` entity'lerini
  yaz. `Infrastructure` projesinde `RegulationDbContext` + global query filter
  (TenantId). `dotnet ef migrations add InitialCreate` ile ilk migration'ı çıkar,
  `CREATE EXTENSION vector` migration'a dahil et.
- **Neden**: EF Core + multi-tenant query filter mantığını kavramadan worker
  tarafında "neden IgnoreQueryFilters kullanıyoruz" sorusu anlamsız kalır.
- **Doğrulama**: Migration Aspire'ın ayağa kaldırdığı Postgres'e uygulanıyor,
  pgAdmin veya `psql` ile tabloları ve `vector` extension'ı görebiliyorsun.
- **Claude'a gel**: Entity + DbContext + migration dosyalarını review ettir,
  özellikle query filter + TenantId izolasyonu doğru mu diye.

**Durum**: ✅ Review edildi (2026-09-09)
**Yapılanlar**:
- `Domain` projesinde `ProductStatus` (enum: Pending/Approved/Rejected), `Product`
  (Id, TenantId, Name, Price, Category, Quantity, Status, CreatedAt — hepsi Guid/
  string/decimal/int/DateTime, `enum` ile `class` farkı ve neden `Guid` (int değil)
  kullanıldığı tartışılarak), `Tenant` (Id, ShopName, Email, CreatedAt), `ProductValidation`
  (Id, ProductId, TenantId, IsCompliant, CategoryMismatch, PriceAnomalyScore,
  Reasoning, Violations: List<string>, ValidatedAt) yazıldı.
- `Infrastructure` projesine `Npgsql.EntityFrameworkCore.PostgreSQL`,
  `Microsoft.EntityFrameworkCore.Design`, `Pgvector`, `Pgvector.EntityFrameworkCore`
  paketleri eklendi; `Domain`'e `ProjectReference` verildi.
- `RegulationDbContext` yazıldı: `DbSet<Product> Products`, `DbSet<ProductValidation>
  ProductValidations`, `DbSet<Tenant> Tenants`, constructor'da `Guid currentTenantId`
  alıp `OnModelCreating`'de `Product`/`ProductValidation` üzerinde
  `HasQueryFilter(x => x.TenantId == _currentTenantId)` ile global tenant izolasyonu.
- `RegulationDbContextFactory : IDesignTimeDbContextFactory<RegulationDbContext>`
  eklendi — `dotnet ef migrations add` gibi tasarım-zamanı komutların, runtime'da
  DI'dan gelecek `currentTenantId`'yi bilmeden de context oluşturabilmesi için
  (sahte `Guid.Empty` + placeholder connection string ile).
- `dotnet ef migrations add InitialCreate` çalıştırıldı → `Products`,
  `ProductValidations`, `Tenants` tabloları üretildi (migration dosyası incelendi,
  doğru). `CREATE EXTENSION vector` bilinçli olarak bu migration'a eklenmedi —
  henüz `Embedding` (pgvector) alanı yok, o Gün 4'te ayrı migration'la gelecek.
- Karşılaşılan ve çözülen hatalar:
  1. Boş klasörler (`Domain`/`Infrastructure`) yanlış isimle (`Regualtion` yazım
     hatası) ve `dotnet new` yerine elle oluşturulmuştu — silinip `dotnet new classlib`
     ile doğru isimle yeniden oluşturuldu.
  2. `dotnet new classlib` kök klasörden çalıştırılınca projeler yanlış yere
     (`AI Applied Engineer - .NET/src/...` yerine `.../01-marketplace-ai-regulation-agent/src/...`)
     oluştu — dosyalar doğru yere taşındı, kalıntı `bin/obj` kilitleri temizlendi.
  3. `class Product` yerine `enum Product` yazılmıştı (enum = sabit değer listesi,
     class = veri taşıyan nesne farkı konuşuldu); property syntax hataları
     (`{get; set;};` fazladan `;`, Türkçe `ı` karakteri, `dateTime`/`guid` küçük
     harf tip adları) tek tek düzeltildi.
  4. `ProductValidation : Product` (kalıtım) yazılmıştı — kavramsal hata: bir
     validation kaydı bir ürünün "türü" değil, ona sadece `ProductId` ile referans
     verir (association, inheritance değil). Kaldırıldı.
  5. `RegulationDbContext.cs`'te `DbSet` property'leri yanlışlıkla constructor'ın
     gövdesi içine yazılmıştı (bir metodun içine property tanımlanamaz); sonra
     `OnModelCreating` da aynı şekilde eski constructor'ın içine gömülmüştü —
     dosya class-seviyesinde tek constructor + class-seviyesinde `DbSet`'ler +
     class-seviyesinde `OnModelCreating` olacak şekilde yeniden düzenlendi.
  6. `dotnet ef migrations add`, constructor'daki `Guid currentTenantId`
     parametresini dolduramadığı için "Unable to resolve service" hatası verdi —
     `IDesignTimeDbContextFactory` eklenerek çözüldü (yukarıda açıklandı).
- Zaman baskısı nedeniyle bu gün, entity'lerin ilk yazımı sen tarafından (soru-cevap
  yöntemiyle), `RegulationDbContext`'in ilk taslağı ve migration/factory düzeltmeleri
  ise doğrudan Claude tarafından yapıldı (normalde review-only rolün dışına çıkıldı,
  bilinçli bir istisna).
**Doğrulama sonucu**: `dotnet build` (Domain + Infrastructure) 0 hata/0 uyarı ile
geçti. `dotnet ef migrations add InitialCreate` başarıyla 3 tablo üretti. Migration'ın
gerçek Aspire-Postgres'e uygulanması (`dotnet ef database update`), Docker bu ortamda
o an kapalı olduğu için Gün 3'e (Api/Worker ilk çalıştığında) bırakıldı.
**Review notları**: Domain modelleme süreci iyi geçti (enum/class, kalıtım/ilişki,
string/List, Guid/int ayrımları kavrandı). EF Core/DbContext kısmı zaman baskısıyla
hızlandırıldı — bu kısmın (`OnModelCreating`, design-time factory) mantığını daha
sakin bir oturumda tekrar okuyup kendi kelimelerinle özetlemek faydalı olur.
![alt text](image.png)
---

## Gün 3 — Contracts ve mesajlaşma iskeleti (stub validator ile)

- **Yap**: `Contracts` projesinde `ProductSubmitted`, `ProductValidated` record'ları
  ve `ProductValidationResult`/`ViolationFinding` DTO'larını yaz. Api ve Worker'a
  MassTransit + RabbitMQ bağla. Api'de `POST /products` endpoint'i: DB'ye kaydet,
  `ProductSubmitted` yayınla. Worker'da `ProductSubmittedConsumer`: **şimdilik
  sahte/sabit bir sonuç üret** (ör. her zaman `IsCompliant = true`), DB'ye yaz,
  `ProductValidated` yayınla.
- **Neden**: Uçtan uca mesaj akışını AI/pgvector karmaşıklığı olmadan önce
  kanıtlamak — bir şey bozulursa hangi katmanda olduğunu bilirsin.
- **Doğrulama**: `curl` ile ürün gönder, worker loglarında consume edildiğini gör,
  DB'de `ProductValidation` satırının oluştuğunu doğrula.
- **Claude'a gel**: Consumer + endpoint kodunu review ettir; MassTransit retry/error
  queue konfigürasyonunu birlikte gözden geçirin.

**Durum**: ✅ Review edildi (2026-09-09)
**Yapılanlar**:
- `Contracts` projesi: `ProductSubmitted(ProductId, TenantId, SubmittedAt)` ve
  `ProductValidated(ProductId, TenantId, IsCompliant, CategoryMismatch,
  PriceAnomalyScore, Reasoning, Violations)` record'ları yazıldı. `ProductSubmitted`
  bilinçli minimal tutuldu (worker DB'den okuyabildiği için); `ProductValidated`
  ise bilinçli olarak tam detay taşıyor (dinleyen servisler DB'ye erişmesin diye,
  event-driven mimaride tercih edilen bir yaklaşım).
- `Api` ve `ValidationWorker` projeleri oluşturuldu (`dotnet new webapi` / `worker`),
  `ServiceDefaults`/`Contracts`/`Domain`/`Infrastructure`'a referans verildi.
  `AppHost.cs` güncellendi: her iki proje `AddProject` ile eklendi, `regulationDb`
  ve `rabbitmq`'ya `WithReference`/`WaitFor` bağlandı.
- `ICurrentTenantAccessor` arayüzü (`Infrastructure`) eklendi; `RegulationDbContext`
  artık `Guid` yerine bu arayüzü alıyor. Api'de `HttpTenantAccessor` (X-Tenant-Id
  header'ından okuyor), Worker'da `MessageTenantAccessor` (mutable, consumer
  mesajın TenantId'sini buraya yazıyor) implementasyonları yazıldı.
- `Api/Program.cs`: `POST /products` endpoint'i — `Product` oluştur, `db.Products.Add`,
  `SaveChangesAsync`, `publishEndpoint.Publish(new ProductSubmitted(...))`,
  `Results.Accepted(...)` dön. Ayrıca uygulama başlarken bekleyen migration'ları
  otomatik uygulayan bir blok eklendi (`RegulationDbContext.Database.MigrateAsync()`,
  sabit/boş bir `StartupTenantAccessor` ile — migration'lar tenant'tan bağımsız
  şema işlemleri).
- `ValidationWorker/ProductSubmittedConsumer.cs`: mesajı alıp `MessageTenantAccessor`'ı
  doldur → `_db.Products`'tan ürünü bul → sahte/sabit sonuç (`IsCompliant = true`)
  üret → `ProductValidation` oluştur, `_db.ProductValidations.Add` → `SaveChangesAsync`
  → `context.Publish(new ProductValidated(...))`.
- **Uçtan uca doğrulandı**: AppHost + Docker ile tüm sistem çalıştırıldı, `curl`
  ile `POST /products` çağrıldı → `202 Accepted` → Postgres'e doğrudan bağlanıp
  (`docker exec ... psql`) kontrol edildi: `Product.Status = Approved (1)`,
  `ProductValidations` tablosunda `IsCompliant = true`, `Reasoning = "Sahte
  doğrulama sonucu: her zaman uyumlu"` satırı gerçekten oluşmuş.
- Karşılaşılan ve çözülen hatalar (bu gün en çok altyapı/paket sorunuyla geçti):
  1. Yanlış dizinden çalıştırılan `dotnet new`/`dotnet add reference` komutları
     yüzünden `Contracts` projesi `Infrastructure/src/...` altına gömülü oluştu —
     doğru yere taşındı, kalıntı klasör (`Infrastructure/src`) derlemede tuhaf
     "duplicate assembly attribute" (CS0579) hatalarına yol açtı, silinince düzeldi.
  2. `record ProductSubmitted` dosyasının içeriği yanlışlıkla `ProductValidated`
     koduyla ezilmişti (kopyala-yapıştır hatası) — iki dosya da aynı tip'i
     tanımlıyordu, düzeltildi.
  3. Minimal API'de top-level statement sıralaması: `app.MapPost(...)`,
     `var app = builder.Build();`'dan ÖNCE yazılmıştı (derleme hatası);
     ve bir `record` tanımı, çalıştırılabilir satırlardan önce kalmıştı
     (CS8803) — ikisi de doğru sıraya alındı.
  4. EF Core paket sürüm çakışması (`Microsoft.EntityFrameworkCore` 10.0.11 vs
     10.0.12, farklı paketler farklı sürüm istiyordu) → CPM'in
     `CentralPackageTransitivePinningEnabled` özelliği açılıp merkezi sürümler
     (`Microsoft.EntityFrameworkCore`, `.Relational`) eklendi.
  5. `MassTransit` 9.2.1 çalışma zamanında lisans anahtarı istedi
     (`ConfigurationException: License must be specified`) — ücretsiz/açık kaynak
     `8.1.3` sürümüne sabitlendi.
  6. `MassTransit.RabbitMQ` 8.1.3, `RabbitMQ.Client`'ın eski (6.x) API şekline
     bağımlı; ama `Aspire.RabbitMQ.Client` (kullanılmıyordu, kaldırıldı) ve
     `Aspire.Hosting.RabbitMQ` (AppHost'ta gerekli) `RabbitMQ.Client 7.2.1+`
     istiyordu → `CentralPackageVersionOverrideEnabled` açılıp merkezi sürüm
     7.2.1'de bırakıldı, sadece Api/Worker'da `VersionOverride="6.8.1"` ile
     geçersiz kılındı (AppHost ve Api/Worker ayrı process'ler, farklı sürüm
     kullanmaları sorun değil).
  7. Aspire'ın `AddNpgsqlDbContext` yardımcı metodu `RegulationDbContext`'i
     "pooled" kaydediyor; pooled context'ler Scoped bağımlılık
     (`ICurrentTenantAccessor`) kabul etmiyor (`Cannot resolve scoped service
     ... from root provider`) → standart `AddDbContext` + `UseNpgsql`'e geçildi.
  8. **En önemli mantık hatası**: `RegulationDbContext`, `tenantAccessor.TenantId`'yi
     constructor'da bir `Guid` alanına KOPYALIYORDU. Ama DI, `RegulationDbContext`'i
     `ProductSubmittedConsumer`'ın constructor'ında, `Consume()` metodu (ve oradaki
     `MessageTenantAccessor.TenantId = ...` ataması) çalışmadan ÖNCE oluşturuyor —
     yani filtre hep boş/varsayılan tenant'a göre çalışıyordu ("Product not found"
     hatası). Düzeltme: kopya yerine `ICurrentTenantAccessor` referansının kendisi
     saklandı, filtre `_tenantAccessor.TenantId`'yi her sorguda canlı okuyor.
  9. Migration hiçbir zaman gerçek Postgres'e uygulanmamıştı (`relation "Products"
     does not exist`) — Api başlangıcında otomatik `Database.MigrateAsync()`
     çağrısı eklendi.
- Zaman baskısı nedeniyle bu gün de (Gün 2'deki gibi) altyapı/wiring kısımları
  (AppHost güncellemesi, Program.cs'ler, paket/sürüm düzeltmeleri) büyük ölçüde
  Claude tarafından yazıldı; entity/mesaj tasarım kararları (ProductSubmitted'ın
  minimal, ProductValidated'ın detaylı olması gibi) soru-cevap yöntemiyle
  kullanıcı tarafından verildi.
**Doğrulama sonucu**: Tam uçtan uca test geçti (yukarıda detaylı). MassTransit
retry/error queue konfigürasyonu (planın "Claude'a gel" notunda geçen) henüz
elle ayarlanmadı — MassTransit'in varsayılan retry davranışıyla bırakıldı,
ileride (belki Gün 6/7'de) gözden geçirilebilir.
**Review notları**: Domain/mesaj tasarımı sağlam. Bu günün asıl öğretici hatası
#8 (tenant filter closure bug'ı) — "DI constructor'ları ne zaman çalışır"
konusunun canlı bir örneği oldu. Paket sürüm çakışmalarının çoğu (5, 6, 7) bu
projeye özel değil, MassTransit + Aspire + EF Core'u aynı anda kullanan her
projede karşılaşılabilecek genel bilgi — not olarak faydalı.

---

## Gün 4 — pgvector ile benzerlik araması

- **Yap**: `Product.Embedding` alanını `Pgvector.Vector` tipiyle ekle, EF Core
  mapping'ini yap (`vector(N)` kolon tipi + `ivfflat`/`hnsw` index migration'ı).
  Şimdilik gerçek embedding modeli yerine basit/deterministik bir sahte vektör
  üretici yaz (ör. metin hash'inden türetilen sabit uzunlukta float array).
  Worker'da en yakın "reddedilmiş" ürünleri bulan sorguyu (`OrderBy(...L2Distance)`)
  yaz.
- **Neden**: pgvector sorgu mantığını (mesafe fonksiyonu, index, LINQ çevirisi)
  gerçek bir LLM embedding modeline bağımlı olmadan test edebilmek için.
- **Doğrulama**: Birkaç test ürünü (biri "reddedilmiş" olarak işaretli) seed et,
  benzer bir ürün gönderdiğinde sorgunun doğru komşuyu döndürdüğünü doğrula.
- **Claude'a gel**: pgvector sorgusu + index migration'ını review ettir.

**Durum**: ✅ Review edildi (2026-09-13)
**Yapılanlar**:
- `Product.Embedding` eklendi. Önce `float[]?` (Domain saf kalsın, Infrastructure'da
  `HasConversion` ile `Pgvector.Vector`'a dönüştürülsün) diye tasarlandı, ama
  `Pgvector.EntityFrameworkCore`'un `L2Distance` gibi LINQ-çevrilebilir sorgu
  metodlarının doğrudan `Vector` tipi üzerinde tanımlı olduğu (float[] üzerinde
  değil) ortaya çıkınca, bilinçli bir trade-off ile `Product.Embedding` doğrudan
  `Pgvector.Vector?` yapıldı (Domain artık `Pgvector` paketine bağımlı — saflık
  prensibinden ödün verildi, gerekçesi PROGRESS.md'de tartışıldı: kütüphanenin
  sunduğu hazır sorgu API'sini kullanmak, hem daha az kod hem "ham SQL yazıp
  SQL injection riski almaktan" daha güvenli).
- `RegulationDbContext.OnModelCreating`: `Product.Embedding` için
  `HasColumnType("vector(768)")` + `modelBuilder.HasPostgresExtension("vector")`.
- `IProductEmbeddingGenerator` arayüzü + `FakeEmbeddingGenerator` implementasyonu
  (ikisi de `Infrastructure`) — metnin `GetHashCode()`'unu `Random` seed'i olarak
  kullanıp deterministik 768 boyutlu bir `Vector` üretiyor (aynı metin = aynı
  vektör, farklı metin = alakasız/rastgele vektör — gerçek anlamsal benzerlik
  YOK, bu Gün 5'e kadar bilinen bir sınırlama).
- Worker DI'a `AddSingleton<IProductEmbeddingGenerator, FakeEmbeddingGenerator>`
  eklendi (stateless olduğu için Scoped değil Singleton — tartışıldı).
- `ProductSubmittedConsumer`: ürün bulunduktan sonra embedding hesaplanıyor,
  ardından `_db.Products.Where(Status==Rejected && Embedding!=null)
  .OrderBy(L2Distance).Take(5).Select(Name)` ile en yakın 5 reddedilmiş ürün
  bulunuyor, sonuç `reasoningText`'e (hem `ProductValidation.Reasoning` hem
  `ProductValidated` mesajı için tek kaynak) ekleniyor.
- `AddProductEmbedding` migration'ı oluşturuldu (`vector(768)` kolonu +
  `CREATE EXTENSION vector` annotation'ı) ve gerçek Postgres'e uygulandı.
- **Uçtan uca doğrulandı**: bir ürün gönderildi, elle `Status=Rejected`
  işaretlendi (stub validator hiç reddetmediği için organik yol yok), sonra
  BİREBİR aynı isim/kategoriyle ikinci bir ürün gönderildi (fake generator'ın
  "aynı metin = aynı vektör" özelliğini kullanarak kesin eşleşme testi) →
  ikinci ürünün `Reasoning`'inde "Benzer reddedilmiş ürünler: Sahte Marka Saat"
  çıktığı doğrulandı — pgvector sorgu zinciri (LINQ → SQL → sonuç) tam çalışıyor.
- Karşılaşılan ve çözülen hatalar:
  1. `RegulationDbContextFactory` ve Api/Worker'ın `UseNpgsql(...)` çağrılarında
     `o => o.UseVector()` eksikti — "Vector properties cannot be mapped" hatası
     verdi, üç yere de eklendi (Npgsql'e pgvector tipini tanıtan ayar).
  2. `modelBuilder.HasPostgresExtension("vector");` satırı yanlışlıkla
     `OnModelCreating` metodunun DIŞINA, class gövdesine yazılmıştı (metod
     dışında çalıştırılabilir kod olamaz) — içeri taşındı.
  3. Domain'de `float[]` → Infrastructure'da `Vector`'a `HasConversion` ile
     dönüştürme planı, `L2Distance`'ın `Vector` üzerinde tanımlı olması
     (float[] üzerinde değil) yüzünden terk edildi — yukarıda anlatıldı.
  4. `L2Distance`'ın hangi paketten/namespace'ten geldiği belirsizdi
     (`Pgvector` mi `Pgvector.EntityFrameworkCore` mi) — normal IDE/derleyici
     ipuçları çelişkili çıkınca, geçici bir konsol projesiyle reflection
     kullanılıp gerçek namespace (`Pgvector.EntityFrameworkCore`,
     `VectorDbFunctionsExtensions.L2Distance`, `object` üzerinde extension
     metod) doğrudan tespit edildi.
  5. `ValidationWorker` projesine `Pgvector.EntityFrameworkCore` paketi hiç
     eklenmemişti (sadece `Infrastructure`'da vardı) — eklendi.
  6. Çeşitli küçük yazım hataları (`_db. Product` boşluklu/eksik "s",
     `p.Embeddings` vs `p.Embedding` tutarsızlığı, arayüzün içine gövde
     yazılması — "default interface method" kavramı bu vesileyle konuşuldu).
- Bu gün, önceki günlerin aksine **kod yazımının çoğunu kullanıcı yaptı**,
  Claude çoğunlukla review + paket/namespace keşfi (#4) gibi altyapı
  sorunlarında yardımcı oldu — Gün 2/3'teki dengesizlik bu günde düzeldi.
**Doğrulama sonucu**: Yukarıda detaylı — tam uçtan uca, gerçek Postgres'e karşı
test edildi, sonuç doğru.
**Review notları**: `float[]` → `Vector` kararı iyi bir örnek: "saf Domain"
prensibi ile "kütüphanenin sunduğu güvenli/hazır API'yi kullanmak" çatışınca,
ikincisi seçildi ve gerekçesi kayıt altına alındı — bu tarz bilinçli, açıklanmış
trade-off'lar, kör bir kural takibinden daha olgun bir mühendislik duruşu.

---

## Gün 5 — Ollama'yı Aspire'a ekle, gerçek embedding + chat modeline geç

- **Yap**: AppHost'a Ollama container'ını ekle (risk #1'i burada çöz —
  `Aspire.Hosting.Ollama` var mı diye NuGet'te kontrol et, yoksa `AddContainer`
  ile manuel kur). Model pull adımını (chat: `llama3.1:8b`, embedding:
  `nomic-embed-text`) ekle. Worker'da sahte embedding üreticiyi gerçek
  `IEmbeddingGenerator` çağrısıyla değiştir.
- **Neden**: Container orkestrasyonu + model indirme akışını, AI çağrısı katmanına
  geçmeden önce stabilize etmek.
- **Doğrulama**: Aspire dashboard'da `ollama` resource'u yeşil, loglarda iki
  modelin de pull edildiğini gör; worker artık gerçek embedding üretiyor
  (log'a vektör uzunluğunu yazdırarak doğrula).
- **Claude'a gel**: AppHost'taki Ollama wiring'i ve model-pull stratejisini
  review ettir (bu adım riskli, muhtemelen birlikte debug edeceğiz).

**Durum**: ✅ Review edildi (2026-09-13)
**Yapılanlar**:
- Zaman baskısı nedeniyle model `llama3.1:8b` yerine `llama3.2:3b`'ye
  düşürüldü (kullanıcıyla onaylanan bir karar) — daha küçük indirme (~2GB),
  daha hızlı yanıt.
- `Aspire.Hosting.Ollama` diye resmi bir paket yok; onun yerine topluluk
  paketi `CommunityToolkit.Aspire.Hosting.Ollama` (13.5.0, Aspire 13.5.x ile
  uyumlu) kullanıldı. Metod isimlerini tahmin etmek yerine (geçen seferki
  `L2Distance` hatasından ders alınarak) paket, geçici bir konsol projesiyle
  reflection kullanılarak incelendi — gerçek API: `builder.AddOllama(name)`,
  `.AddModel(name, modelName)`, `.WithDataVolume()`.
- `AppHost.cs`: `ollama` resource'u + iki model (`chat` → llama3.2:3b,
  `embeddings` → nomic-embed-text), `validation-worker`'a `WithReference`/
  `WaitFor` ile bağlandı.
- Client tarafı için `CommunityToolkit.Aspire.OllamaSharp` paketi (yine
  reflection ile API'si doğrulandı) — `Microsoft.Extensions.AI`'ın
  `IEmbeddingGenerator<string, Embedding<float>>` soyutlamasını Ollama'ya
  bağlıyor: `builder.AddOllamaApiClient("embeddings").AddEmbeddingGenerator()`.
- `RealEmbeddingGenerator : IProductEmbeddingGenerator` (Infrastructure) —
  `FakeEmbeddingGenerator`'ın (Gün 4) yerini aldı, `IEmbeddingGenerator`'ı
  sarıp `GenerateVectorAsync` çağırıyor, sonucu `Pgvector.Vector`'a çeviriyor.
  `ProductSubmittedConsumer` hiç değişmedi — arayüz aynı kaldığı için (Gün
  4'te tasarlanan `IProductEmbeddingGenerator` soyutlamasının tam faydası
  burada görüldü).
- Worker DI kaydı `FakeEmbeddingGenerator` → `RealEmbeddingGenerator` olarak
  değiştirildi.
- **Uçtan uca doğrulandı**: AppHost çalıştırıldı, Ollama image + iki model
  indi (`docker exec ... ollama list` ile doğrulandı: `llama3.2:3b` 2.0GB,
  `nomic-embed-text` 274MB), tüm 8 resource (ollama, chat, embeddings,
  postgres, regulationdb, rabbitmq, api, validation-worker) yeşil. Bir ürün
  gönderildi, worker'ın gerçekten Ollama'ya embedding isteği attığı ve
  sonucu `vector(768)` kolonuna yazdığı doğrulandı (kolon tipi sabit
  olduğu için boyut uyuşmasaydı INSERT hata verirdi — vermedi).
- Karşılaşılan ve çözülen hatalar:
  1. **Docker Desktop WSL2 DNS arızası**: `ollama` image'ı ilk denemede
     "lookup auth.docker.io: no such host" hatasıyla inemedi — host
     makinenin DNS'i çalışıyordu ama Docker Desktop'ın WSL2 sanal makinesi
     çözemiyordu. `wsl --shutdown` (WSL2 ağ yığınını sıfırlar) + Docker
     Desktop'ı yeniden başlatmakla çözüldü. Bununla ilgili container hiç
     oluşmadan "kayboluyordu" gibi görünen kafa karıştırıcı bir ara durum
     da yaşandı (image pull yarıda kesilince container hiç create edilmemiş
     oluyor, `docker ps -a`'da bile görünmüyor).
  2. `IEmbeddingGenerator<string, Embedding<float>>` ve `GenerateVectorAsync`
     imzaları `Microsoft.Extensions.AI` sürümleri arasında değişebildiği
     için (bilinen risk #5, PROGRESS.md'nin başında), yine reflection ile
     gerçek API doğrulandı, tahmin edilmedi.
- Bu gün, altyapı kısmı (paket keşfi + AppHost/DI wiring) yine büyük ölçüde
  Claude tarafından yapıldı — plandaki "Gün 5 riskli, Claude daha aktif
  yardımcı olur" notuyla tutarlı, bilinçli bir istisna.
**Doğrulama sonucu**: Yukarıda detaylı — 8/8 resource yeşil, gerçek embedding
üretimi uçtan uca çalışıyor.
**Review notları**: `IProductEmbeddingGenerator` arayüzünü Gün 4'te doğru
tasarlamış olmamızın faydası tam burada ortaya çıktı — sahteyi gerçekle
değiştirmek tek bir DI satırı + yeni bir implementasyon dosyasıydı,
`ProductSubmittedConsumer`'a hiç dokunulmadı. Bu, "arayüz arkasında
programlama" prensibinin somut, kanıtlanmış bir faydası.

---

## Gün 6 — Gerçek LLM çağrısı ve typed structured output

- **Yap**: `Microsoft.Extensions.AI` + Ollama OpenAI-uyumlu endpoint ile `IChatClient`
  kur (risk #3/#4'ü burada doğrula). `ProductValidationPromptBuilder` yaz (policy
  kuralları + ürün + Gün 4/5'teki benzer ürün sonuçları). Stub validator'ı
  `chatClient.GetResponseAsync<ProductValidationResult>(prompt)` ile değiştir.
- **Neden**: Projenin can alıcı kısmı — typed AI output'u üretimde nasıl
  kullanacağını öğrenmek.
- **Doğrulama**: Açıkça uyumsuz bir ürün (ör. "iPhone 15 Pro Max - 5 TL") gönder,
  `IsCompliant: false` ve yüksek `PriceAnomalyScore` dönmeli. Normal bir ürün
  için `IsCompliant: true` dönmeli. Birkaç "golden case" ile elle test et.
- **Claude'a gel**: Prompt tasarımını ve typed output mapping'ini review ettir;
  bozuk JSON / şema uyuşmazlığı çıkarsa birlikte debug edelim.

**Durum**: ✅ Review edildi (2026-09-15)
**Yapılanlar**:
- `ProductValidationResult` (Contracts): `IsCompliant`, `Violations`, `CategoryMismatch`,
  `PriceAnomalyScore`, `Reasoning` — AI'ın üreteceği tipli sonuç şeması. Bilinçli
  olarak `ProductValidation` (DB entity) ile aynı değil: kimlik/zaman bilgisi
  yok, AI'ın bunları "uydurmasını" istemiyoruz.
- `IChatClient` Worker'a kaydedildi (`builder.AddOllamaApiClient("chat").AddChatClient()`).
- `ProductValidationPromptBuilder` (raw string literal ile çok satırlı prompt) —
  ürün bilgisi + Gün 4'teki benzer reddedilmiş ürünler + 3 kriter (yasal/kategori/fiyat).
- `ProductSubmittedConsumer` güncellendi: stub (`isCompliant = true`) tamamen
  kaldırıldı, yerine `_chatClient.GetResponseAsync<ProductValidationResult>(prompt)`
  → `result.IsCompliant`'e göre `Product.Status` (Approved/Rejected) belirleniyor.
- API doğrulaması, plan dosyasında `GetResponseAsync<T>` için tahmin edilmişti
  (risk #4) — reflection ile doğrulandı: `ChatClientStructuredOutputExtensions.
  GetResponseAsync<T>(...)` → `Task<ChatResponse<T>>`, `.Result` ile tipli nesne.
- **Gerçek, kritik bir altyapı sorunu bulundu ve çözüldü**: `Microsoft.Extensions.
  Http.Resilience`'ın varsayılan 10 saniyelik HTTP zaman aşımı, `llama3.2:3b`'nin
  CPU'da typed/şema-kısıtlamalı JSON üretmesi için (~120 saniyeye kadar sürebiliyor)
  çok kısaydı. Önce tek bir HttpClient'a özel `Configure<HttpStandardResilienceOptions>
  (clientName, ...)` ile düzeltmeye çalışıldı — **işe yaramadı** (muhtemelen yanlış
  named-options anahtarı). Sonra `ServiceDefaults/Extensions.cs`'teki genel
  `AddStandardResilienceHandler()` çağrısına doğrudan `AttemptTimeout=90sn,
  TotalRequestTimeout=120sn, CircuitBreaker.SamplingDuration=180sn` (Api'yi
  etkilemez çünkü Api'nin böyle yavaş bir dış çağrısı yok) eklenerek **kesin
  şekilde** çözüldü.
- Teşhis için: Aspire'ın yönettiği worker süreci durdurulup, aynı Worker
  **elle**, Aspire'ın container'larından çıkarılan gerçek bağlantı bilgileriyle
  (`docker port`, `docker exec ... printenv`) terminalde doğrudan çalıştırıldı
  — böylece worker'ın konsol çıktısı (normalde sadece OTLP ile dashboard'a giden,
  tarayıcı aracı o an bağlanamadığı için erişilemeyen loglar) doğrudan görülebildi.
  Ayrıca hatalı mesajı yeniden işletmek için `rabbitmqadmin publish` ile
  RabbitMQ'nun error queue'suna düşen mesaj elle ana kuyruğa geri gönderildi.
- **Uçtan uca doğrulandı (gerçek AI karar verdi)**: "Silah Kilifi" (weapon
  holster) adlı bir test ürünü gönderildi. Model, 2. denemede (~119 saniyede,
  ilk deneme 90sn'de zaman aşımına uğradı) doğru şekilde `IsCompliant: false`,
  `CategoryMismatch: true` döndürdü ve `Reasoning`'de "silahlardan oluştuğu için
  yasaklı içerik barındırmaktadır" diye **gerçekten anlamlı bir gerekçe** üretti.
  `Product.Status` veritabanında `Rejected (2)` olarak güncellendi — stub değil,
  gerçek model kararı sistemi yönetiyor.
- Bilinen, bloklamayan eksikler (Gün 7'ye not): `PriceAnomalyScore` modelin
  kendi ürettiği bir sayı (5), 0-1 aralığına normalize edilmemiş — prompt'a
  "0.0-1.0 arası bir sayı" talimatı eklenmeli. `Violations` listesi boş kaldı,
  model bulguları `Reasoning`'e yazdı — prompt'a "her ihlali Violations
  listesine ayrı ayrı ekle" talimatı eklenmeli.
**Doğrulama sonucu**: Yukarıda detaylı — gerçek model çağrısı, gerçek karar,
gerçek DB güncellemesi doğrulandı. Performans notu: yerel CPU'da typed/şema
kısıtlamalı çıktı için 90-120 saniyelik bir HTTP zaman aşımı **gerekli**,
varsayılan 10 saniye yetersiz — bu proje için kalıcı bir mimari karar.
**Review notları**: Bu gün, planın en riskli maddesiydi ("bozuk JSON / şema
uyuşmazlığı çıkarsa birlikte debug edelim" diye not edilmişti) ve gerçekten
öyle çıktı — ama sorun beklenen "bozuk JSON" değil, "zaman aşımı" idi. İlk
düzeltme denemesinin (özel isimli `Configure`) sessizce işe yaramaması,
"düzeltmenin gerçekten etkili olduğunu doğrulamadan bir sonraki adıma
geçmemek" ilkesinin önemini bir kez daha gösterdi — ikinci, daha kaba ama
garanti bir yöntemle (genel `ServiceDefaults` ayarı) sorun kesin çözüldü.

---

## Gün 7 — API sorgu uçları, cilalama, demo hazırlığı

- **Yap**: `GET /products/{id}` ve `GET /products/{id}/validation` endpoint'lerini
  yaz. OpenAPI/Swagger (veya Scalar) UI ekle. Bu README.md'ye (proje klasöründe)
  nasıl çalıştırılacağını yaz.
- **Neden**: Portföyde gösterilecek son hâl; recruiter'ın README okuyup 2 dakikada
  ne olduğunu anlaması lazım.
- **Doğrulama**: Sıfırdan `dotnet run` ile tüm sistem ayağa kalkıyor, README'deki
  adımları takip eden biri (ör. Claude) ürün gönderip sonucu görebiliyor.
- **Claude'a gel**: Son review — kod kalitesi, README netliği, demo akışını
  birlikte uçtan uca test edelim.

**Durum**: ✅ Review edildi (2026-09-15)
**Yapılanlar**:
- `GET /products/{id}`: `Products` tablosunda `Id` + tenant filtresiyle ara,
  bulunamazsa `404`, bulunursa entity'yi değil yeni bir `ProductResponse`
  record'unu (`Id, Name, Category, Price, Quantity, Status`) döndür — entity'nin
  `Embedding` gibi iç alanlarının API sözleşmesine sızmaması için.
- `GET /products/{id}/validation`: iki aşamalı kontrol — önce ürün var mı diye
  bak (yoksa gerçek hata → `404`), varsa `ProductValidations`'ta `ProductId`
  eşleşmesi ara (henüz yoksa bu bir hata değil, asenkron pipeline'ın doğal bir
  ara durumu → `200 { "status": "Pending" }`), bulunursa yeni bir
  `ValidationResponse` record'una (`IsCompliant, CategoryMismatch,
  PriceAnomalyScore, Reasoning, Violations`) map'leyip dön.
- İlk yazımda iki bug çıktı, ikisi de review'da yakalanıp düzeltildi:
  (1) `/validation` endpoint'i kopyala-yapıştır kalıntısıyla `ProductValidations`
  tablosuna hiç dokunmuyor, hep `Products`'a bakıyordu; (2) DTO'ya geçildikten
  sonra da "validation bulundu" dalı hâlâ `validation` değil `product`'tan
  `ProductResponse` üretiyordu — yani endpoint hiçbir zaman gerçek AI kararını
  dönmüyordu, testte fark edilmeseydi sessizce yanlış kalacaktı.
- Scalar UI + OpenAPI doküman üretimi eklendi (Claude tarafından, bilinçli bir
  istisna — bu kısım büyük ölçüde paket/konfig boilerplate'i, öğrenme değeri
  düşük): `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` paketleri,
  `builder.Services.AddOpenApi()`, ve `Development` ortamında
  `app.MapOpenApi()` + `app.MapScalarApiReference()` (`/scalar/v1`).
- Proje `README.md`'si tamamen yenilendi: güncel mimari diyagramı (query
  endpoint'leri dahil), Scalar UI linki, `POST /products` → `GET .../validation`
  uçtan uca demo akışı, ve "Bilinen sınırlamalar" bölümü (aşağıya bak).
- Kök `README.md`'deki proje tablosu Gün 1-7 tamamlandı olarak güncellendi.

**Doğrulama sonucu**: Var olan container'lara (Postgres/RabbitMQ, Gün 6'dan
kalan) bağlanıp Api'yi doğrudan terminalden çalıştırarak dört senaryo elle
test edildi:
1. `GET /products/{id}` (var olan ürün, "Silah Kilifi") → `200`, `embedding`
   alanı yanıtta yok.
2. `GET /products/{id}/validation` (validate edilmiş ürün) → `200`, gerçek AI
   kararı (`isCompliant:false, categoryMismatch:true, ...`) dönüyor.
3. `GET /products/{id}` (olmayan id) → `404`.
4. `GET /products/{id}/validation` (ürün var, worker henüz işlememiş — DB'de
   `ProductValidations` kaydı yok) → `200 { "status": "Pending" }`.
`dotnet build` 0 hata/0 uyarı ile geçti. Scalar UI (`/scalar/v1`) ve OpenAPI
doküman (`/openapi/v1.json`) endpoint'leri de `200` döndü.

**Review notları**:
- İki bug da ("yanlış tabloyu sorgulama", "DTO'ya geçerken yanlış kaynaktan
  map'leme") review adımında, kod build olduğu ve syntax olarak "doğru
  göründüğü" hâlde yakalandı — ikisi de derleyicinin yakalayamayacağı türden
  mantık hataları, sadece davranışı bilerek okuyunca ya da gerçek veriyle test
  edince ortaya çıkıyor. Bu, "build başarılı" ile "doğru çalışıyor"un aynı şey
  olmadığının iyi bir örneği.
- `PriceAnomalyScore` ve `Violations` konusundaki Gün 6'dan kalma prompt
  eksiklikleri hâlâ duruyor (bu Gün 7'nin testinde de tekrar gözlemlendi,
  `reasoning` metninde bu sefer Türkçe-İngilizce karışık bozuk bir cümle de
  vardı) — bunlar API katmanıyla değil, `ProductValidationPromptBuilder`'daki
  talimatların netliğiyle ilgili. Bilinçli olarak bu proje kapsamının dışında
  bırakıldı (projenin 7 günlük planı burada tamamlanıyor); ileride ayrı bir
  iyileştirme olarak ele alınabilir.
- Tenant izolasyonu her iki endpoint'te de hem `HasQueryFilter` (global, otomatik)
  hem açık `&& p.TenantId == tenantAccessor.TenantId` koşuluyla (redundant ama
  zararsız) sağlanıyor — kullanıcı bunun neden gerekli olmadığını sorguladı,
  bilinçli olarak "açıkça görünsün" tercihiyle bıraktı.

---

## Roller

- **Kullanıcı**: Her günün kodunu bizzat yazar. Takıldığında spesifik soru sorar —
  Claude tam çözümü direkt yazmaz, önce neyin yanlış olduğunu açıklar.
- **Claude**: Mimarı belirledi, her gün sonunda review yapar, riskli adımlarda
  (Gün 5, Gün 6) daha aktif yardımcı olur.
- **Git**: Her gün sonunda küçük, anlamlı bir commit (`git commit -m "Day N: ..."`).

## Proje bitince genel doğrulama

1. `dotnet run` AppHost'tan — tüm resource'lar (postgres, rabbitmq, ollama, api,
   validation-worker) dashboard'da yeşil.
2. Bir uyumsuz, bir uyumlu ürün gönder, iki farklı sonucu doğrula.
3. Aspire dashboard'da distributed trace'in Api → RabbitMQ → Worker → Postgres/Ollama
   zincirini gösterdiğini kontrol et.
4. Kök `README.md`'deki proje tablosuna bu projeyi ekle (durum: tamamlandı).
