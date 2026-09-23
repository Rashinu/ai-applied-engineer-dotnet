# LLM FinOps Copiloti

> Durum: 🚧 Geliştiriliyor — ilerleme için [PROGRESS.md](./PROGRESS.md)'ye bak.

Şirketler agentic AI'a geçtikçe LLM API maliyetleri kontrolden çıkıyor (retry
fırtınaları, tekrar eden prompt'lar, cache kullanılmaması). Bu proje, LLM
kullanım loglarından maliyet tahmini yapan, maliyet sıçramalarını erken
yakalayan ve hangi prompt'ların cache'lenebilir/sıkıştırılabilir olduğunu
**kanıta dayalı** (grounded) öneren bir analiz servisi.

## Mimari

```
Veri katmanı: sentetik LLM çağrı logları (Postgres)
        │
        ▼
ML.NET SSA forecasting ──┐
ML.NET anomali tespiti ──┼──► Guarded LLM analyst (Microsoft.Extensions.AI)
Embedding + K-Means   ───┘      → önerilerini yalnızca sayısal kanıta
                                   referansla üretir, kanıtsız iddia reddedilir
```

Detaylı mimari, gün gün/blok blok geliştirme süreci ve alınan kararlar için:
[PROGRESS.md](./PROGRESS.md).

## Teknolojiler

- .NET 10
- ML.NET (`Microsoft.ML.TimeSeries` — SSA forecasting, IidSpike/IidChangePoint
  anomali tespiti)
- Microsoft.Extensions.AI (embedding + guarded chat client)
- PostgreSQL

## Çalıştırma

Proje henüz erken aşamada (Blok 1 — Domain entity'si). Çalıştırma talimatları
Faz 1 tamamlandığında eklenecek.
