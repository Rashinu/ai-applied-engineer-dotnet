# Multi-Tenant Marketplace AI Regulation Agent

> Durum: 🚧 Geliştiriliyor — ilerleme için [PROGRESS.md](./PROGRESS.md)'ye bak.

Trendyol/Hepsiburada tipi bir pazar yerinde satıcıların yüklediği ürünleri
asenkron olarak denetleyen, yasal/regülasyon ihlallerini, kategori
yanlışlıklarını ve fiyat anomalilerini yakalayan bir servis.

## Mimari

```
Api → RabbitMQ (MassTransit) → ValidationWorker
                                   ├─ pgvector: benzer/reddedilmiş ürün araması
                                   └─ Ollama (IChatClient, typed JSON output)
                                        → PostgreSQL'e sonucu yaz → ProductValidated yayınla
```

Detaylı mimari ve gün gün geliştirme planı: proje kökündeki plan dosyasına
bakılabilir (geliştirme sürecine özel, repoya dahil değil).

## Teknolojiler

- .NET 10, .NET Aspire
- MassTransit + RabbitMQ
- PostgreSQL + pgvector (EF Core + Pgvector.EntityFrameworkCore)
- Microsoft.Extensions.AI + Ollama (typed structured output)

## Çalıştırma

Gerekli: Docker Desktop çalışır durumda olmalı.

```powershell
cd src/MarketplaceRegulationAgent.AppHost
dotnet run
```

Konsolda çıkan Aspire dashboard linkine git. Ayakta olanlar: `postgres`
(pgvector destekli), `regulationdb`, `rabbitmq`, `api`, `validation-worker`.

Bir ürün göndermek için (asenkron akış: Api → RabbitMQ → Worker → Postgres):

```bash
curl -X POST http://localhost:<api-portu>/products \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -d '{"name":"iPhone 15 Pro Max","category":"Electronics/Phones","price":2500,"quantity":10}'
```

Şu an validasyon **sahte/sabit** (her ürün otomatik onaylanıyor) — gerçek AI
entegrasyonu Gün 6'da gelecek (bkz. [PROGRESS.md](./PROGRESS.md)).
