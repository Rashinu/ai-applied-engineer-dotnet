# LLM FinOps Copiloti

> Durum: ✅ Faz 1 tamamlandı — ilerleme/karar günlüğü için [PROGRESS.md](./PROGRESS.md)'ye bak.

Şirketler agentic AI'a geçtikçe LLM API maliyetleri kontrolden çıkıyor (retry
fırtınaları, tekrar eden prompt'lar, cache kullanılmaması). Bu proje, LLM
kullanım loglarından maliyet tahmini yapan, maliyet sıçramalarını erken
yakalayan ve hangi prompt'ların cache'lenebilir/sıkıştırılabilir olduğunu
**kanıta dayalı** (grounded) öneren bir analiz servisi — önerilerini
yalnızca gerçek sayısal kanıta referansla üretir, kanıtsız/uydurma bir
iddia ürettiğinde bunu **kendi kendine tespit edip reddeder**.

## Mimari

```
Veri katmanı (Postgres)
  LlmCallLogs      — sentetik LLM çağrı logları (sezonluk desen + enjekte
                      edilmiş maliyet sıçramaları, ~430K satır)
  AnomalyWindows   — enjekte edilen sıçramaların kalıcı "ground truth"u

ML.NET SSA forecasting ──┐
ML.NET IidSpike anomali ─┼──► Guarded LLM analyst (Microsoft.Extensions.AI)
Embedding + K-Means  ────┘      → typed, kanıta zorunlu öneri üretir;
                                   grounding-check kanıtsız değeri reddeder

Minimal API (GET /insights) — yukarıdaki üçünü tek bir uçtan uca akışta
                                orkestre eder, JSON olarak döner
```

Her modül ayrı bir .NET projesi (`src/` altında); detaylı mimari, blok blok
geliştirme süreci, alınan kararlar, karşılaşılan hatalar ve nasıl
çözüldükleri için: [PROGRESS.md](./PROGRESS.md) — bu proje de Proje 1 gibi
şeffaf bir süreç günlüğü tutuyor, sadece bitmiş ürün değil "nasıl bitti"
de görünür.

## Proje yapısı

| Proje | Sorumluluk |
|---|---|
| `LlmFinOpsCopilotDomain` | Entity'ler (`LlmCallLog`, `AnomalyWindow`) |
| `LlmFinOpsCopilot.Infrastructure` | EF Core `DbContext`, migration'lar |
| `LlmFinOpsCopilot.DataGenerator` | Sentetik veri + anomali üretimi, DB'ye yazma |
| `LlmFinOpsCopilot.Forecasting` | ML.NET SSA forecasting + rolling-origin backtest |
| `LlmFinOpsCopilot.Clustering` | Ollama embedding + K-Means kümeleme |
| `LlmFinOpsCopilot.Analyst` | Guarded LLM: typed öneri + grounding-check (konsol) |
| `LlmFinOpsCopilot.Api` | Yukarıdakileri birleştiren `GET /insights` endpoint'i |

## Teknolojiler

- .NET 10, EF Core + Npgsql (PostgreSQL)
- ML.NET (`Microsoft.ML.TimeSeries` — SSA forecasting, `IidSpikeEstimator`
  anomali tespiti; `Microsoft.ML` — K-Means kümeleme)
- Microsoft.Extensions.AI + OllamaSharp (embedding + guarded chat client,
  Aspire'sız — Ollama doğrudan Docker container'ında çalışır)
- Minimal API (ASP.NET Core)

## Çalıştırma

Gerekli: Docker Desktop çalışır durumda olmalı.

```powershell
# Postgres (ayrı, bu projeye özel container)
docker run -d --name llmfinops-postgres -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=llmfinopscopilotdb -p 5433:5432 postgres:16

# Ollama (ayrı, bu projeye özel container — Proje 1'inkinden bağımsız)
docker run -d --name llmfinops-ollama -p 11435:11434 `
  -v llmfinops-ollama-data:/root/.ollama ollama/ollama
docker exec llmfinops-ollama ollama pull nomic-embed-text
docker exec llmfinops-ollama ollama pull llama3.2:3b
```

Sonra sırayla (migration zaten `Infrastructure` projesinde tanımlı,
ilk `dotnet ef database update` ile uygulanır):

```powershell
cd src/LlmFinOpsCopilot.Infrastructure
dotnet ef database update

cd ../LlmFinOpsCopilot.DataGenerator
dotnet run          # ~430K sentetik log + anomali pencerelerini üretir, DB'ye yazar

cd ../LlmFinOpsCopilot.Api
dotnet run
```

API ayağa kalkınca (varsayılan `http://localhost:5107`):

```bash
curl http://localhost:5107/insights
```

Örnek yanıt:

```json
{
  "trend": 0.91,
  "anomalyCount": 6,
  "mostFrequentPrompt": "This is a sample prompt 55",
  "mostFrequentPromptCount": 4481,
  "recommendation": "Geçen hafta maliyet, önceki haftaya göre %0,91 değişti...",
  "evidenceType": "Trend",
  "evidenceValue": 0.91,
  "isGrounded": true
}
```

`isGrounded: false` dönerse, LLM'in önerisi kanıtsız bir sayıya dayanıyor
demektir — sistem bunu reddediyor, bu bilinçli ve gösterilmesi gereken bir
davranış (bkz. PROGRESS.md'deki Blok 10-12, canlı halüsinasyon örneği).

## Bilinen sınırlamalar

- **Sentetik veri**: Tüm veri (loglar, prompt'lar, anomaliler) üretilmiş,
  gerçek bir şirketin verisi değil. Amaç gerçek iş değeri değil, metodolojiyi
  (forecasting + anomali tespiti + kümeleme + guarded LLM) doğru kurup
  ölçmek.
- **Forecasting**: Rolling-origin backtest MAPE ~%8,7 (eğitim verisindeki
  sıçramalar medyanla temizlendikten sonra); tek bir sabit eşik (3× medyan)
  kullanıldı, farklı veri setlerinde yeniden ayarlanması gerekebilir.
- **Anomali tespiti**: Precision %57 / Recall %83 — yanlış pozitiflerin
  büyük kısmı gerçek sıçramaların hemen öncesi/sonrasındaki geçiş saatleri,
  ground truth penceresi bunları kapsamıyor (test edilmemiş bir hipotez).
- **Guarded LLM**: Küçük, yerel bir model (`llama3.2:3b`) kullanılıyor —
  tutarsız davranabiliyor (bazen alan eksik bırakıyor, bazen sayıyı
  prompt'taki alakasız bir rakamla karıştırıyor). Grounding-check tam da bu
  yüzden var; büyük bir modelle (GPT-4 sınıfı) hata oranı muhtemelen
  düşer ama proje bilinçli olarak yerel/ücretsiz bir modelle sınandı.
- **Faz 2 (dashboard, Docker Compose, CI/CD quality gate) kapsam dışı
  bırakıldı** — bilinçli bir karar (28 Ekim 2026), detayı PROGRESS.md'de.
