# Release-Risk Çok-Ajanlı Analist

> Durum: ✅ Tamamlandı — ilerleme/karar günlüğü için [PROGRESS.md](./PROGRESS.md)'ye bak.

Bir yazılım release'inin risk seviyesini, farklı açılardan bakan üç uzman
ajanın (kod değişikliği, test/CI sinyalleri, geçmiş incident kayıtları)
bulgularını birleştirerek değerlendiren çok-ajanlı bir sistem. Microsoft
Agent Framework (ajan orkestrasyonu) ve MCP — Model Context Protocol
(ajanların veriye hazır araçlar üzerinden erişmesi) bu projede ilk kez
kullanıldı.

## Mimari

```
Sentetik veri (Postgres)
  ReleaseSignal, ServiceIncidentHistory

MCP Sunucusu — ajanlar DB'ye doğrudan değil, tanımlı ARAÇLAR üzerinden erişir
  get_release_signals(serviceName) / get_service_incident_histories(serviceName)

Microsoft Agent Framework — 3 uzman ajan + 1 orkestratör
  Kod Risk Ajanı      → linesChanged/reviewerCount/testCoverage/failedCiRunCount
  Test Risk Ajanı     → testCoverage/failedCiRunCount
  Geçmiş Risk Ajanı   → incident severity dağılımı
  Orkestratör         → nihai Level/BasedOnAgents KODDA (deterministik
                         çoğunluk kuralı) hesaplanır, LLM sadece bu kararı
                         gerekçelendiren bir paragraf yazar

Minimal API — GET /risk-assessment/{serviceName}, tek birleşik endpoint
```

Detaylı mimari, faz faz geliştirme süreci, karşılaşılan gerçek hatalar ve
nasıl çözüldükleri için: [PROGRESS.md](./PROGRESS.md) — bu proje de
Proje 1/2 gibi şeffaf bir süreç günlüğü tutuyor.

## Proje yapısı

| Proje | Sorumluluk |
|---|---|
| `ReleaseRiskAnalyst.Domain` | Entity'ler (`ReleaseSignal`, `ServiceIncidentHistory`) |
| `ReleaseRiskAnalyst.Infrastructure` | EF Core `DbContext`, migration'lar |
| `ReleaseRiskAnalyst.DataGenerator` | Sentetik veri üretimi (gerçek risk korelasyonuyla) |
| `ReleaseRiskAnalyst.McpServer` | MCP sunucusu — `get_release_signals`/`get_service_incident_histories` tool'ları |
| `ReleaseRiskAnalyst.Agents` | Faz 4-6 kanıt-doğrulama konsol uygulaması — 3 ajan + orkestratör, sabit bir servis adıyla |
| `ReleaseRiskAnalyst.Api` | Aynı mantığın dinamik servis adı kabul eden Minimal API hali — `GET /risk-assessment/{serviceName}` |

## Teknolojiler

- .NET 10, EF Core + Npgsql (PostgreSQL)
- Microsoft.Agents.AI (Microsoft Agent Framework) + OllamaSharp
- ModelContextProtocol (MCP C# SDK) — hem sunucu hem client tarafı
- Minimal API (ASP.NET Core)

## Çalıştırma

Gerekli: Docker Desktop çalışır durumda olmalı.

```powershell
# Postgres (ayrı, bu projeye özel container)
docker run -d --name releaserisk-postgres -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=releaseriskdb -p 5434:5432 postgres:16

# Ollama (ayrı, bu projeye özel container)
docker run -d --name releaserisk-ollama -p 11436:11434 ollama/ollama
docker exec releaserisk-ollama ollama pull llama3.2:3b
```

Sonra sırayla:

```powershell
cd src/ReleaseRiskAnalyst.Infrastructure
dotnet ef database update

cd ../ReleaseRiskAnalyst.DataGenerator
dotnet run          # 300 sentetik release + gerçek risk korelasyonlu incident üretir

cd ../ReleaseRiskAnalyst.Api
dotnet run
```

MCP sunucusunu elle başlatmana gerek yok — `Api` (ve `Agents`) projesi
onu bir alt-process olarak (`dotnet run --project ...McpServer.csproj`)
kendisi, stdio üzerinden başlatıyor.

API ayağa kalkınca (varsayılan `http://localhost:5017`):

```bash
curl http://localhost:5017/risk-assessment/payment-service
```

DataGenerator'ın ürettiği gerçek servis adları: `payment-service`,
`checkout-service`, `inventory-service`, `auth-service`.

Örnek yanıt (gerçek bir çalıştırmadan, kısaltılmış):

```json
{
  "serviceName": "payment-service",
  "kodRiskAjani": { "level": 2, "reason": "Lines changed is 1000. Reviewer count is 1. Test coverage is 60. Failed CI run count is 5." },
  "testRiskAjani": { "level": 0, "reason": "testCoverage %50'nin altı" },
  "gecmisRiskAjani": { "level": 2, "reason": "..." },
  "nihaiKarar": {
    "level": 2,
    "reason": "...",
    "basedOnAgents": "Kod Risk Ajanı ve Geçmiş Risk Ajanı"
  }
}
```

`level`: `0` = Low, `1` = Medium, `2` = High (bilinçli olarak sayısal
bırakıldı, string'e çevrilmedi). `nihaiKarar.level` ve `basedOnAgents`,
LLM'den değil, üç ajanın `level` değerini karşılaştıran deterministik bir
C# fonksiyonundan geliyor — `reason` alanı ise LLM'in bu zaten verilmiş
kararı gerekçelendirdiği serbest metin.

## Bilinen sınırlamalar

- **Sentetik veri**: Tüm veri (release sinyalleri, incident kayıtları)
  üretilmiş, gerçek bir şirketin verisi değil.
- **Küçük model, sert kısıtlamalar gerektirdi**: `llama3.2:3b`, CPU
  üzerinde ~6 token/sn gibi yavaş bir hızda çalışıyor ve serbest metin
  talimatlara ("kısa tut", "şemayı tarif etme") güvenilir şekilde
  uymuyor. Bu proje boyunca tekrar eden ders: **format ve kritik kararları
  modelden değil koddan iste** — `MaxOutputTokens` ile sert üst sınır,
  enum ile kısıtlı değer kümesi, ve en kritik olarak nihai risk kararının
  kendisinin (orkestratörün "level" alanı) modelden değil C#'tan gelmesi.
- **Yapılandırılmış çıktı, tool'suz ajanlarda güvenilmez bulundu**:
  Orkestratör (hiçbir MCP tool'u çağırmayan tek ajan) `GetResponseAsync<T>`
  ile istenen bir enum alanını JSON'da defalarca tamamen atladı — non-
  nullable enum sessizce ilk değerine (`Low`) düşüyordu, bu da yanlış bir
  karar gibi görünen sessiz bir hataya yol açtı. Tool çağıran uzman
  ajanlarda aynı mekanizma güvenilir çalıştı. Kesin sebebi doğrulanmadı,
  ama gözlem tutarlıydı (bkz. PROGRESS.md Faz 6).
- **Risk kalibrasyonu**: Modele "yüksek/düşük" gibi göreceli ifadeler
  yerine somut sayısal eşikler (`linesChanged > 1000`, `testCoverage <
  %50` gibi) verilmeden önce, model neredeyse her girdiye "High" diyordu.
  Somut eşikler bu sorunu büyük ölçüde çözdü, ama kusursuz değil — model
  bazen kendi belirttiği sayılarla (örn. `testCoverage: 80%`) tutarsız bir
  seviye söyleyebiliyor.
- **Her sorguda yeniden hesaplama**: API, her istekte üç ajanı + orkestratörü
  sıfırdan çalıştırıyor (önbellek/geçmiş sorgu tekrar kullanımı yok) —
  CPU'da yavaş model nedeniyle bir istek tipik olarak birkaç dakika sürüyor.
  Üretim kullanımı için önbellekleme gerekir, bu proje kapsamında ele
  alınmadı.
