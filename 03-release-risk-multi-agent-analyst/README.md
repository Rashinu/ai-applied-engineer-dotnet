# Release-Risk Çok-Ajanlı Analist

> Durum: 🚧 Geliştiriliyor — ilerleme için [PROGRESS.md](./PROGRESS.md)'ye bak.

Bir yazılım release'inin risk seviyesini, farklı açılardan bakan birkaç
uzman ajanın bulgularını birleştirerek değerlendiren çok-ajanlı bir sistem.
Microsoft Agent Framework (ajan orkestrasyonu) ve MCP — Model Context
Protocol (ajanların veriye araçlar üzerinden erişmesi) bu projede ilk kez
kullanılıyor.

## Mimari

```
Sentetik veri (Postgres)
  ReleaseSignal, ServiceIncidentHistory

MCP Sunucusu — ajanlar DB'ye doğrudan değil, tanımlı araçlar üzerinden erişir
  get_release_signals / get_service_incident_history

Microsoft Agent Framework — 3 uzman ajan + 1 orkestratör
  Kod Risk Ajanı / Test Risk Ajanı / Geçmiş Risk Ajanı → Orkestratör
  (kanıta dayalı tek bir karar, hangi ajanın bulgusuna dayandığını belirtir)

Minimal API — tek, birleşik endpoint
```

Detaylı mimari, faz faz geliştirme süreci ve alınan kararlar için:
[PROGRESS.md](./PROGRESS.md).

## Teknolojiler

- .NET 10, EF Core + Npgsql (PostgreSQL)
- Microsoft.Agents.AI (Microsoft Agent Framework)
- ModelContextProtocol (MCP C# SDK)

## Çalıştırma

Proje henüz erken aşamada (Faz 1-2 kısmi). Çalıştırma talimatları proje
ilerledikçe eklenecek.
