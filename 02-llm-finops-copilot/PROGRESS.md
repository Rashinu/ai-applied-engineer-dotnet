# İlerleme Günlüğü — LLM FinOps Copiloti

Kural: Bir bloğun **Durum**'u ✅ Review edildi olmadan bir sonraki bloğa geçilmez.
"Yapılanlar / Doğrulama" kısmını sen doldurursun, "Review notları" kısmını ben.

**Tempo notu**: "Blok N" etiketleri takvim günü değil — kişisel haftalık plandaki
sabit .NET kod bloklarından (Salı 19:30-21:00, Cumartesi 11:30-13:30, Pazar
19:30-21:00 — haftada ~5 saat garanti coding) her birine karşılık geliyor.

**Kapsam kararı (23 Eylül 2026, önceki tur konuşmasında alındı)**: Bu proje
**Faz 1** olarak scope'landı — dashboard (Blazor/React) ve CI/CD quality gate
kapsam dışı bırakıldı. Faz 1 bitince (Blok 15), o noktadaki zamana/enerjiye
göre Faz 2'ye devam edilip edilmeyeceğine ayrıca karar verilecek. Süre tahmini:
~19-26 saat gerçek kodlama, 5 saat/hafta bütçeyle **4-5 hafta** (Proje 1'deki
gibi bonus oturumlarla kısalabilir).

**Neden CRUD değil**: Zaman serisi tahmini (ML.NET SSA), değişim noktası/anomali
tespiti (IidSpike/IidChangePoint), embedding tabanlı kümeleme (K-Means) ve bu
üçünü sentezleyip **kanıta bağlı** öneri üreten bir "guarded" LLM katmanı
birbirine bağlı — LLM, üç modülden dönen sayısal kanıta (forecast değeri,
anomali skoru, küme büyüklüğü) referans vermeden öneri üretemiyor.

| Blok | Tarih | Hafta günü / oturum |
|------|-------|----------------------|
| 1 | 2026-09-23 | Çarşamba (kaçırılan Salı bloğu buraya kaydırıldı) |

---

## Mimari (referans — tüm bloklar boyunca sabit)

```
Veri katmanı
  LlmCallLog (Postgres) — Provider, Model, PromptTokenCount, CompletionTokenCount,
  LatencyMs, Cost, CacheHit, Team, Prompt (embedding için), Timestamp
  Sentetik üretici: sezonluk desen + enjekte edilmiş maliyet sıçraması (agent
  retry fırtınası senaryosu), ~300-500K satır

ML/AI katmanı
  ML.NET SSA (TimeSeriesCatalog.ForecastBySsa) — model/takım bazlı maliyet tahmini
  ML.NET IidSpike/IidChangePoint (TimeSeriesCatalog.DetectIidSpike/DetectIidChangePoint)
    — maliyet anomalisi tespiti, enjekte edilen sıçramalara karşı etiketli test seti
  Microsoft.Extensions.AI IEmbeddingGenerator — prompt embedding + K-Means kümeleme
    → tekrar eden/benzer prompt gruplarını (cache fırsatları) bulma
  Microsoft.Extensions.AI IChatClient "guarded analyst" — önerilerini sadece
    yukarıdaki üç modülden dönen sayısal kanıta referansla üretir; kanıtsız
    iddia tespit edilirse reddedilir (grounding-check eval script'i ile doğrulanır)

Minimal API — üç modülü orkestre eden endpoint'ler (Blok 13-14)
```

## Bilinen Riskler / Madde 0'da Doğrulananlar

Blok 1'de doğrulandı (reflection ile, throwaway probe projesiyle — Proje 1'de
öğrenilen teknik):
- `Microsoft.ML` 5.0.0 + `Microsoft.ML.TimeSeries` 5.0.0, `net10.0` ile
  sorunsuz restore oluyor.
- `TimeSeriesCatalog.ForecastBySsa(...)`, `.DetectIidSpike(...)`,
  `.DetectIidChangePoint(...)`, `.DetectSpikeBySsa(...)`,
  `.DetectChangePointBySsa(...)` — hepsi gerçek, planlanan şekilde mevcut.

Doğrulanmamış, ileride dikkat edilecek:
- Bu API'lerin gerçek kullanımı (parametre ayarlama — `windowSize`,
  `seriesLength`, `confidence` vs.) — paket varlığı risk değil ama doğru
  kullanım öğrenme eğrisi gerektirebilir (Blok 4-5, 7-8).
- Sentetik veri enjeksiyon mantığının modelin "bilmeden" bulması gereken
  bağımsız bir rastgelelik taşıması — aksi halde precision/recall ölçümü
  "kendi sınavını kendin yazmak" olur (bkz. Proje 2 review konuşması).

---

## Taslak Blok Planı (Faz 1)

| Hafta | Blok | Oturum | İçerik |
|---|---|---|---|
| 1 | 1 | Salı 1,5sa (bu sefer Çarşamba'ya kaydı) | Proje iskeleti + Madde 0 + `LlmCallLog` entity'si |
| 1 | 2 | Cumartesi 2sa | Sentetik veri üretici: sezonluk desen + enjekte edilmiş maliyet sıçraması |
| 1 | 3 | Pazar 1,5sa | Veri üreticiyi bitir (~300-500K satır), doğrula, commit |
| 2 | 4 | Salı 1,5sa | ML.NET SSA forecasting pipeline kurulumu |
| 2 | 5 | Cumartesi 2sa | Forecasting'i bitir + backtesting harness (MAPE/sMAPE) |
| 2 | 6 | Pazar 1,5sa | Backtest sonuçlarını değerlendir/ayarla, commit |
| 3 | 7 | Salı 1,5sa | Anomali tespiti (IidSpike/ChangePoint) kurulumu |
| 3 | 8 | Cumartesi 2sa | Etiketli test setiyle precision/recall ölçümü + clustering'e başla |
| 3 | 9 | Pazar 1,5sa | Clustering'i bitir, cache-fırsatı skorlama, commit |
| 4 | 10 | Salı 1,5sa | Guarded LLM analyst: prompt tasarımı + IChatClient wiring |
| 4 | 11 | Cumartesi 2sa | Grounding-check eval script'i |
| 4 | 12 | Pazar 1,5sa | Eval'i golden-case'lerle test et, commit |
| 5 | 13 | Salı 1,5sa | Minimal API — üç modülü orkestre eden endpoint'ler |
| 5 | 14 | Cumartesi 2sa | Uçtan uca test, düzeltmeler |
| 5 | 15 | Pazar 1,5sa | README + LinkedIn taslağı, Faz 1 tamam |

Not: Bu iskelet kesin değil — Proje 1'de olduğu gibi ilerledikçe revize edilir.

---

## Blok 1 — Proje iskeleti + Madde 0 + LlmCallLog entity'si

- **Yap**: `02-llm-finops-copilot/` klasörü, `global.json`, `.slnx`,
  `Directory.Build.props`, `Directory.Packages.props`, `LlmFinOpsCopilotDomain`
  class library, içinde `LlmCallLog` entity'si (provider, model, prompt/
  completion token sayısı, gecikme, maliyet, cache-hit, team). Madde 0: ML.NET
  paketlerinin .NET 10 ile gerçekten uyumlu olduğunu doğrula.
- **Neden**: Proje 1'de öğrendiğimiz gibi, paket/versiyon riskini en başta
  görmek — kodun gövdesine geçmeden önce zemin sağlam olmalı.
- **Doğrulama**: `dotnet build` 0 hata/0 uyarı.
- **Claude'a gel**: Entity'nin plandaki alan listesiyle örtüştüğünü review et.

**Durum**: ✅ Review edildi (2026-09-23)
**Yapılanlar**:
- Proje iskeleti kullanıcı tarafından `dotnet new globaljson`/`dotnet new sln`
  ile oluşturuldu.
- `LlmCallLog` entity'si üç review turunda kullanıcı tarafından yazıldı ve
  düzeltildi:
  1. İlk taslak genel bir "chat log" şeklindeydi (UserId, Prompt, Response,
     Timestamp) — FinOps'a özgü hiçbir alan (Provider, Model, token sayısı,
     maliyet, cache-hit) yoktu.
  2. İkinci turda Provider/Model/TokenCount(tek alan)/Latency/CacheHit/Team
     (`string[]` olarak, yanlış) eklendi.
  3. Üçüncü turda: `Cost` alanı `decimal`'e çevrildi (`double` yerine — parasal
     değerler için ikili kayan nokta hassasiyet riski taşır), `Team` tek
     `string`'e indirildi (bir çağrı tek takıma ait), `TokenCount` ikiye
     bölündü (`PromptTokenCount`/`CompletionTokenCount` — sağlayıcılar input/
     output token'ı farklı fiyatlandırdığı için ayrı tutulmaları maliyet
     hesaplamasının doğruluğu için zorunlu), tüm `string` alanlara
     `= string.Empty;` varsayılan değeri eklendi (Proje 1'deki `Product.Name`
     deseni), `Id` `string`'ten `Guid`'e çevrildi, `Latency` → `LatencyMs`
     olarak yeniden adlandırıldı (birim netliği için, kullanıcının kendi
     inisiyatifiyle).
- Claude tarafından (config/boilerplate, düşük öğrenme değeri — Proje 1'deki
  Gün 1/Gün 7'deki aynı istisna): `Directory.Build.props` (net10.0, Nullable/
  ImplicitUsings, CPM ayarları), `Directory.Packages.props` (Microsoft.ML +
  Microsoft.ML.TimeSeries 5.0.0 sabitlendi), proje `.slnx`'e eklendi.
- Madde 0 doğrulaması Claude tarafından yapıldı: throwaway bir probe
  konsol projesiyle (`Assembly.LoadFrom` + reflection, Proje 1'de öğrenilen
  teknik) `Microsoft.ML.TimeSeries` 5.0.0'ın gerçek tip/metod yüzeyi
  doğrulandı — planlanan `ForecastBySsa`, `DetectIidSpike`,
  `DetectIidChangePoint`, `DetectSpikeBySsa`, `DetectChangePointBySsa`
  metodlarının hepsi mevcut ve planlanan şekilde çalışıyor; Proje 1'deki
  RabbitMQ.Client/MassTransit türü bir versiyon çakışması bu paketlerde yok.

**Doğrulama sonucu**: `dotnet build` — ilk denemede `Id` hâlâ `string`
olduğu için CS8618 uyarısı verdi (beklenen, entity'nin eksik olduğunu
doğruladı); `Id` `Guid`'e çevrildikten sonra 0 uyarı/0 hata ile geçti.

**Review notları**:
- Entity, üç review turu sonunda plandaki alan listesiyle (provider, model,
  ayrı prompt/completion token sayısı, gecikme, maliyet, cache-hit) tam
  örtüşüyor. Her tur bir önceki turun bıraktığı en az bir maddeyi kaçırdı
  (örn. Guid dönüşümü iki tur boyunca unutuldu) — bu normal, önemli olan
  her seferinde build'in (CS8618 uyarısı) gerçek durumu doğru yansıtması
  oldu; derleyici burada bir tür "otomatik review" görevi gördü.
- Madde 0'ın sonucu Proje 1'e göre çok daha rahat çıktı — ML.NET paketlerinde
  hiçbir versiyon/API sürprizi yok. Asıl risk muhtemelen ileride (Blok 4-5,
  7-8) bu API'lerin doğru parametrelerle kullanılmasında çıkacak, paket
  varlığında değil.

---

## Roller

- **Kullanıcı**: Her bloğun kodunu bizzat yazar. Takıldığında spesifik soru
  sorar — Claude tam çözümü direkt yazmaz, önce neyin yanlış olduğunu açıklar.
- **Claude**: Mimarı belirledi, her blok sonunda review yapar, config/
  boilerplate işlerinde (Directory.Build.props vb.) ve Madde 0 risk
  doğrulamasında doğrudan yardımcı olur.
- **Git**: Her blok grubunun sonunda (genelde haftalık, bir önceki projede
  olduğu gibi) küçük, anlamlı bir commit.
