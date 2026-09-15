# Multi-Tenant Marketplace AI Regulation Agent

> Durum: ✅ Gün 1-7 tamamlandı — uçtan uca çalışan, gerçek yerel LLM kararı üreten bir
> sistem. Gelişim süreci için [PROGRESS.md](./PROGRESS.md)'ye bak.

Trendyol/Hepsiburada tipi bir pazar yerinde satıcıların yüklediği ürünleri
**asenkron** olarak denetleyen, yasal/regülasyon ihlallerini, kategori
yanlışlıklarını ve fiyat anomalilerini bir yerel LLM'e (Ollama) sorup
**typed/structured output** olarak alan bir servis. Multi-tenant: her satıcı
(tenant) sadece kendi ürünlerini görür, tenant izolasyonu EF Core global
query filter ile veritabanı katmanında zorunlu kılınır.

## Mimari

```
Api (minimal API)
  POST /products                    → DB'ye yaz → ProductSubmitted yayınla (RabbitMQ)
  GET  /products/{id}                → ürün bilgisi
  GET  /products/{id}/validation     → AI karar sonucu (yoksa "Pending")

        │  RabbitMQ (MassTransit)
        ▼

ValidationWorker (arka plan servisi, MassTransit consumer)
  ProductSubmittedConsumer:
    1. Ürünü yükle, embedding üret (Ollama: nomic-embed-text)
    2. pgvector: geçmişte reddedilmiş en benzer 5 ürünü bul (L2Distance)
    3. Prompt oluştur (ürün + benzer reddedilenler + 3 değerlendirme kriteri)
    4. IChatClient.GetResponseAsync<ProductValidationResult>(prompt)
       → Ollama (llama3.2:3b), typed/schema-constrained JSON çıktı
    5. Sonucu Postgres'e yaz, Product.Status güncelle, ProductValidated yayınla

PostgreSQL + pgvector    — ürünler, validation sonuçları, embedding vektörleri
Ollama                   — chat modeli (llama3.2:3b) + embedding modeli (nomic-embed-text)
.NET Aspire AppHost      — tüm servisleri orkestre eder, dashboard'da izlenebilir
```

Detaylı gün gün geliştirme süreci, alınan mimari kararlar ve karşılaşılan
sorunlar/çözümler için: [PROGRESS.md](./PROGRESS.md).

## Teknolojiler

- .NET 10, .NET Aspire (orkestrasyon + servis keşfi + OpenTelemetry)
- MassTransit + RabbitMQ (asenkron mesajlaşma)
- PostgreSQL + pgvector (EF Core + `Pgvector.EntityFrameworkCore`, `L2Distance` benzerlik araması)
- Microsoft.Extensions.AI + Ollama (`IChatClient.GetResponseAsync<T>` ile typed structured output)
- Scalar (OpenAPI doküman UI'ı)

## Çalıştırma

Gerekli: Docker Desktop çalışır durumda olmalı (Postgres, RabbitMQ ve Ollama
container'ları Aspire tarafından otomatik ayağa kaldırılır — ilk çalıştırmada
Ollama modellerinin indirilmesi birkaç dakika sürebilir).

```powershell
cd src/MarketplaceRegulationAgent.AppHost
dotnet run
```

Konsolda çıkan Aspire dashboard linkine git. Ayakta olması gerekenler:
`postgres` (pgvector destekli), `regulationdb`, `rabbitmq`, `ollama`, `api`,
`validation-worker`.

### API dokümantasyonu (Scalar)

Api çalışırken (Development ortamında) `http://<api-portu>/scalar/v1`
adresinden interaktif API dokümantasyonuna erişilebilir.

### Bir ürün gönder

```bash
curl -X POST http://localhost:<api-portu>/products \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{"name":"Silah Kilifi","category":"Electronics/Test","price":50,"quantity":1}'
```

Yanıt `202 Accepted` döner, ürün `Location` header'ındaki id ile hemen
oluşturulur ama henüz `Pending` durumdadır — validasyon RabbitMQ üzerinden
arka planda işlenir (yerel LLM inference'ı ~1-2 dakika sürebilir).

### Sonucu sorgula

```bash
curl http://localhost:<api-portu>/products/<id> \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111"

curl http://localhost:<api-portu>/products/<id>/validation \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111"
```

Worker henüz işlemediyse `/validation` `{"status":"Pending"}` döner; işlendiğinde
AI'ın kararını (`isCompliant`, `categoryMismatch`, `priceAnomalyScore`,
`reasoning`, `violations`) içeren gerçek sonucu döner.

## Bilinen sınırlamalar

- `priceAnomalyScore` şu an modelin serbestçe seçtiği bir sayı — 0.0-1.0
  aralığına normalize edilmiş değil.
- Model, bulduğu ihlalleri çoğunlukla `reasoning` metnine yazıyor,
  `violations` listesini her zaman doldurmuyor.
- Bunlar prompt tasarımıyla ilgili, mimariyle ilgili değil — ayrıntı için
  [PROGRESS.md](./PROGRESS.md)'deki Gün 6/7 notlarına bak.
