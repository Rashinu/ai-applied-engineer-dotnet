# İlerleme Günlüğü — Marketplace AI Regulation Agent

Kural: Bir günün **Durum**'u ✅ Review edildi olmadan bir sonraki güne geçilmez.
"Yapılanlar / Doğrulama" kısmını sen doldurursun, "Review notları" kısmını ben.

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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

**Durum**: ⬜ Başlanmadı
**Yapılanlar**:
-
**Doğrulama sonucu**:
**Review notları**:

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
