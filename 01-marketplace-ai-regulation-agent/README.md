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

Konsolda çıkan Aspire dashboard linkine git. Şu an ayakta olanlar: `postgres`
(pgvector destekli), `regulationdb` (postgres içindeki veritabanı), `rabbitmq`
(management arayüzü açık). Api ve ValidationWorker projeleri henüz eklenmedi
(bkz. [PROGRESS.md](./PROGRESS.md)).
