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
| 2 (kısmi) | 2026-09-26 / 2026-09-27 | Cumartesi 2sa + Pazar 1,5sa (tek blok olarak birleşti) |
| 2 (tamamlandı) + 3 | 2026-09-29 | Salı 19:30-21:00+ (blok uzadı — anomali enjeksiyonu + DB entegrasyonu tek oturumda) |
| 4 | 2026-10-03 | Cumartesi (planlı blok) — ML.NET SSA pipeline + baseline MAPE |
| 7 | 2026-10-17 | Cumartesi (planda Salı'ydı, bonus Cuma oturumuyla başlayıp Cumartesi'de bitti) — anomali tespiti: IidSpike + ground truth persistance + precision/recall |
| 8-9 (kısmi) | 2026-10-19 | Pazartesi (planda Pazar'dı, taşındı) — embedding + K-Means kümeleme: çekirdek pipeline bitti, örnek prompt gösterimi Çarşamba'ya bırakıldı |
| 10-12 | 2026-10-24 | Cumartesi (planlı blok) — Guarded LLM: typed structured output + grounding-check, hem geçen hem reddedilen örnekle doğrulandı |

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

## Blok 2 — Sentetik veri üretici: hacim + sezonluk desen + anomali enjeksiyonu

- **Yap**: `LlmFinOpsCopilot.DataGenerator` konsol projesi; 90 günlük bir zaman
  aralığına yayılan, saatlik/haftalık sezonluk desene sahip ~300-500K
  `LlmCallLog` üreten mantık; Provider/Model tutarlılığı; token sayısı/maliyet
  hesaplama.
- **Neden**: Bu üretici, projenin geri kalan tüm ML/AI modüllerinin (Blok 4-12)
  üzerine kurulacağı temel veri seti — gerçekçi olmayan/tutarsız veri, sonraki
  bloklardaki analizleri anlamsızlaştırır.
- **Doğrulama**: `dotnet run` çıktısı tek bir özet satırı ("Toplam N log
  üretildi") vermeli, N 300.000-500.000 aralığında olmalı.
- **Claude'a gel**: Mekanik/derleme hatalarında (döngü yerleşimi, değişken
  kapsamı) ve kavramsal tasarım kararlarında (Provider/Model tutarlılığı,
  neden ayrı token alanları) yardım.

**Durum**: ✅ Review edildi (2026-09-29) — hacim/sezonluk desen kısmı
27 Eylül'de bitmişti (o noktada kısmi commit atılmıştı, kullanıcının isteği
üzerine, blok tam bitmeden — bilinçli bir sapma). Anomali enjeksiyonu
(agent retry fırtınası senaryosu + ground-truth pencere listesi) 29 Eylül
Salı bloğunda tamamlandı.

**Yapılanlar**:
- Proje iskeleti: `dotnet new console` + `dotnet add reference` (Domain'e) +
  `dotnet sln add` — üçünün ne işe yaradığı (`.csproj` = tek proje tanımı,
  `.slnx` = proje listesi, `reference` = projeler arası bağımlılık) ayrıca
  konuşuldu.
- Üretici mantığı, kullanıcı tarafından çok sayıda küçük review turunda
  aşamalı olarak inşa edildi:
  1. Tek bir sabit `LlmCallLog` üretimi → 10 tanelik bir döngüye çevrildi,
     konsola yazdırma eklendi.
  2. Provider ilk başta bağımsız rastgele seçiliyordu, Model ayrı bağımsız
     rastgele seçiliyordu — bu, "Ollama + gpt-4" gibi anlamsız kombinasyonlar
     üretebilirdi. `Dictionary<string, string[]>` (Provider → o Provider'a ait
     modeller) yapısına geçilerek geçersiz kombinasyon **yapısal olarak
     imkansız** kılındı (sonradan doğrulama/reddetme yerine, baştan
     engelleme).
  3. Sabit "10 log" yerine zaman bazlı üretime geçildi: dış döngü 90 günlük
     aralığı saat saat geziyor (`AddHours(1)`), her saat için `HourMultiplier`
     (mesai saati/gece) × `DayMultiplier` (hafta içi/hafta sonu) çarpanlarıyla
     `countThisHour` hesaplanıyor, iç döngü o saatte `countThisHour` kadar log
     üretiyor.
  4. Üç ayrı regresyon/yerleşim hatası review'da yakalandı: `baseRate`
     tanımlanmadan kullanılmıştı (derleme hatası); `Timestamp` bir ara
     tekrar `DateTime.UtcNow`'a dönmüştü (tüm logların aynı ana damgalanması
     — 90 günlük yayılımı geçersiz kılan kritik bir hata); özet
     `Console.WriteLine` satırı sırasıyla iç döngünün içinde, sonra dış
     döngünün içinde kaldı (400K, sonra 2160 kez basıldı) — son yerleşim
     hatası kullanıcının isteği üzerine Claude tarafından doğrudan
     düzeltildi (kullanıcı "kafamı karıştırdı" dedi, tekrar tekrar
     açıklamak yerine direkt düzeltme tercih edildi).
- `baseRate = 185` (saatte ortalama çağrı, çarpanlar öncesi) ve 90 günlük
  aralıkla hedeflenen 300-500K satır aralığına ilk denemede isabet edildi.

**Doğrulama sonucu**: `dotnet run` → `Toplam 407318 log üretildi.` — tek
satır, hedef aralıkta (300K-500K).

**Review notları**:
- Provider/Model tutarlılığı için "sonradan kontrol et" yerine "geçersiz
  durumu baştan imkansız kıl" (Dictionary ile) yaklaşımının seçilmesi iyi bir
  karardı — kullanıcı ilk başta "extra kontrol" (reddet/tekrar dene) seçeneğini
  düşünüyordu, yapısal çözümün neden üstün olduğu (bakım kolaylığı, yeni model
  eklendiğinde otomatik doğru davranış) tartışıldı.
- Bu blokta üç kez aynı türden bir hata deseni tekrarlandı: bir önceki turda
  düzeltilen bir şey (döngü içi/dışı yerleşim, `Timestamp` kaynağı) bir
  sonraki elle yapılan değişiklikte sessizce geri geldi. Girinti (indentation)
  düzensizliğinin bunu görmeyi zorlaştırdığı gözlemlendi — kullanıcıya VS
  Code'un otomatik girinti kısayolu önerildi.
- Anomali enjeksiyonunun ertelenmesi doğru bir kapsam kararı — bugünkü blok
  zaten planlanan 1,5 saatin çok üzerine çıktı (iki oturum birleşti), yeni bir
  kavramı (rastgele zaman pencereleri + ground-truth etiketleme) yorgun/uzamış
  bir oturumda eklemek risk taşırdı.

### Ek: Anomali enjeksiyonu (29 Eylül'de tamamlanan kısım)

**Yapılanlar**:
- `anomalyWindows` (`List<(DateTime start, DateTime end)>`) — ana döngüden
  önce, 5-10 rastgele başlangıç noktası ve 1-5 saatlik süre ile üretiliyor.
  Bu liste aynı zamanda ileride (Blok 7-9) anomali tespitinin precision/
  recall'unu ölçeceğimiz **ground-truth** olacak.
- Ana döngüde, her saat için `foreach` ile `anomalyWindows`'un taranıp o
  saatin bir pencerenin içinde olup olmadığının kontrol edilmesi
  (`isAnomaly` bayrağı); eşleşirse `countThisHour`'un 5-10 kat büyütülmesi.
- Kavramsal olarak yeni gelen noktalar (`foreach`'in `in` anahtar
  kelimesinin sözdizimsel anlamı, tuple alanlarına `.` ile erişim, interface
  uygulama söz dizimi `: IDesignTimeDbContextFactory<T>` — bir sonraki
  bölümde) tek tek, somut örneklerle ayrıca açıklandı.

**Doğrulama sonucu**: `dotnet run` → `Toplam 438699 log üretildi.` (önceki
407.318'den ~30.600 fazla — anomali pencerelerinde üretilen ekstra loglar).

---

## Blok 3 — DB entegrasyonu: Infrastructure projesi + migration + gerçek yazma

- **Yap**: `LlmFinOpsCopilot.Infrastructure` class library (`LlmDbContext`,
  `LlmDbContextFactory`), ilk migration, ayrı bir Postgres container
  (Proje 1'inkinden bağımsız), ve `DataGenerator`'ın ürettiği ~440K logu
  gerçekten bu veritabanına yazması.
- **Neden**: Bu veri seti artık bellekte değil, kalıcı — Blok 4'ten itibaren
  ML.NET modülleri bu tabloyu okuyacak.
- **Doğrulama**: Postgres'te `SELECT COUNT(*) FROM "LlmCallLogs"` üretilen
  log sayısıyla eşleşmeli; birkaç örnek satırda Provider/Model tutarlılığı
  ve `Timestamp`'in 90 günlük aralığa gerçekten yayıldığı gözle
  doğrulanmalı.
- **Claude'a gel**: EF Core/migration mekaniği (Proje 1'in tekrarı, düşük
  öğrenme yükü) ve mekanik hatalarda (proje referansları, connection string,
  Docker) yardım.

**Durum**: ✅ Review edildi (2026-09-29)

**Yapılanlar**:
- `LlmDbContext : DbContext` — tek bir `DbSet<LlmCallLog>`, Proje 1'deki
  `RegulationDbContext`'in çok sadeleştirilmiş hali (multi-tenant değil, query
  filter yok). `base(options)` ile constructor zincirleme, `DbContext`'in
  bağlantı bilgisini nasıl aldığı ayrıca açıklandı.
- `LlmDbContextFactory : IDesignTimeDbContextFactory<LlmDbContext>` —
  Proje 1'deki `RegulationDbContextFactory`'nin sadeleştirilmiş hali;
  interface uygulama söz dizimi (`:` işaretinin base class'tan türetmeyle
  farkı) somut örnekle açıklandı.
- İlk `dotnet ef migrations add InitialCreate` denemesi kullanıcı tarafından
  başarıyla çalıştırılmış (migration dosyaları `LlmCallLog`'un tüm alanlarını
  doğru şekilde yansıtıyor); Claude'un aynı komutu tekrar denemesi doğal
  olarak "migration zaten var" hatası verdi, kullanıcının tarafında zaten
  başarılı olduğu bu şekilde doğrulandı.
- Ayrı bir Postgres container'ı (`llmfinops-postgres`, port 5433, Proje 1'in
  Aspire-yönetimli container'ından bağımsız) Claude tarafından başlatıldı;
  connection string'deki DB adı (`llmfinopscopilotdb`) ile container'daki
  DB adı arasındaki uyumsuzluk fark edilip container içinde ek bir
  `CREATE DATABASE` ile giderildi.
- `dotnet ef database update` ile migration uygulandı, `LlmCallLogs` tablosu
  oluştu.
- `DataGenerator`'ın DB'ye yazma kodu üç review turunda düzeltildi:
  1. İlk yazımda DB yazma bloğu `logs` listesi doldurulmadan **önce**
     duruyordu (henüz boş bir listeyi DB'ye yazmaya çalışıyordu) — kullanıcı
     bunu kendi sezgisiyle ("yanlış yere yazdık gibi hissediyorum") fark etti.
  2. `context`/`db` değişken adı tutarsızlığı ve eksik `using` importları
     ayrı bir turda düzeltildi.
  3. `DataGenerator`'ın `Infrastructure`'a proje referansı eksikti; eklenmeye
     çalışılırken bağımsız bir hata ortaya çıktı — `Infrastructure.csproj`
     kendi kendine (`LlmFinOpsCopilot.Infrastructure.csproj`) referans
     veriyordu (muhtemelen önceki bir `dotnet add reference` komutunun yanlış
     dizinden çalıştırılmasından kalma), bu dairesel bağımlılığa yol açıyordu.
     Claude tarafından tespit edilip kaldırıldı.
- Build, `Microsoft.EntityFrameworkCore.Relational` için bir sürüm çakışması
  uyarısı (MSB3277) veriyor (10.0.4 vs 10.0.12) — engelleyici değil,
  şimdilik bilinen/kabul edilen bir uyarı olarak bırakıldı; istenirse
  `Directory.Packages.props`'a açık bir pin eklenerek kesin çözülebilir.

**Doğrulama sonucu**: `dotnet run` sonrası Postgres'te
`SELECT COUNT(*) FROM "LlmCallLogs"` → `438699` (bellekteki sayıyla birebir
eşleşiyor). Örnek satırlarda Provider/Model tutarlı (`OpenAI`+`gpt-4`,
`Anthropic`+`claude-v1`, `Ollama`+`ollama-model-1` — hiç geçersiz kombinasyon
yok), `Timestamp` değerleri 1 Temmuz 2026'dan başlayarak 90 günlük aralığa
gerçekten yayılmış.

**Review notları**:
- Kullanıcının "yanlış yere yazdık gibi hissediyorum" sezgisi doğru çıktı —
  bu, kod okuma/döngü akışını takip etme becerisinin gelişmekte olduğunun
  iyi bir işareti.
- Dairesel proje referansı hatası, DataGenerator'ın Infrastructure'a
  referans eksikliğiyle ilgisizdi — birbirine karışan iki ayrı sorunu
  ayırt edip doğru olanı teşhis etmek, hata mesajını (MSB4006, dairesel
  bağımlılık) dikkatli okumayı gerektirdi.
- Bu blokta Proje 1'in desenlerinin (DbContext, design-time factory,
  migration) tekrar kullanılması bloğu hızlandırdı — yeni kavram sadece
  `base(options)` constructor zincirleme ve interface söz dizimiydi, gerisi
  zaten bilinen bir kalıptı.

---

## Blok 4 — ML.NET SSA forecasting: pipeline + baseline MAPE

- **Yap**: `LlmFinOpsCopilot.Forecasting` konsol projesi; saatlik toplam
  maliyet serisini Postgres'ten okuyup ML.NET SSA ile 24 saatlik tahmin ve
  doğruluk ölçümü (MAPE).
- **Neden**: Projenin "tahmin" ayağının temeli. Tahmin doğruluğunu ölçmeden
  anomali tespiti ya da öneri katmanı anlamlı bir zemine oturmaz.
- **Doğrulama**: 24 saatlik tahmin üretiliyor, gerçek değerlerle
  karşılaştırılıp MAPE yazdırılıyor.
- **Claude'a gel**: API keşfi, pipeline parametreleri, sonuçların yorumu.

**Durum**: ✅ Review edildi (2026-10-03) — pipeline çalışıyor, baseline ölçüldü.
Parametre optimizasyonu ve çoklu pencere backtest'i Blok 5'e bırakıldı.

**Yapılanlar**:
- Proje iskeleti: `dotnet new console` + `dotnet add reference` (Infrastructure)
  + `dotnet sln add`. İlk denemede proje yanlış dizine (kök) oluştu, taşındı.
- Veri okuma: `LlmCallLogs` üzerinde saat bazlı `GroupBy` + `Sum`. Önce
  `Select` içinde `new DateTime(...)` kurmak EF Core tarafından SQL'e
  çevrilemedi (çalışma zamanı hatası). Düzeltme: veritabanı yalnızca
  `Year/Month/Day/Hour` gruplar ve toplar, `DateTime` ve sıralama
  `ToListAsync()`'ten sonra bellekte yapılır.
- Model: `ForecastBySsa` (windowSize 24, eğitim = son 168 saatin ilk 144'ü,
  horizon 24). `decimal` → `float` dönüşümü ML.NET için zorunlu.
- Tahmin: `CreateTimeSeriesEngine` ile `Predict()`. Bu API
  `Microsoft.ML.Transforms.TimeSeries` namespace'inde.
- Ölçüm: son 24 saat gerçek değerlerle karşılaştırıldı, MAPE hesaplandı.

**Doğrulama sonucu**: Baseline MAPE **%17,27** (windowSize 24, 168 saat eğitim
penceresi). Build ve çalıştırma hatasız.

**Deneyler**:
- `windowSize: 48` (diğer her şey aynı): MAPE **%106,79**. Pencere eğitim
  verisine göre çok büyük kaldığı için (144 saatte yalnızca ~97 örnek pencere)
  model kararsızlaştı. Bu bir hipotez, doğrulanmadı. Ayarı 24'e geri alındı.
- Eğitim verisi tüm geçmiş (2160 saat) yerine son 168 saat yapıldığında,
  ilk denemedeki 12-70 arası dalgalı tahmin, 12-23 arası düzgün bir tahmine
  döndü. Bu da eski verideki anomali pencerelerinin ya da uzun eğitim
  penceresinin tahmini bozduğunu düşündürüyor, ama iki değişkenin etkisi
  ayrılmadı.

**Review notları**:
- Sistematik sapma: model gündüz (09-16) değerlerini düşük tahmin ediyor
  (~20-21 vs gerçek ~28). Gece saatlerinde de hafif düşük. Yani genlik
  sönümleniyor, seviye aşağıda kalıyor.
- Tek 24 saatlik pencere istatistiksel olarak zayıf bir ölçüm. Blok 5'te
  rolling backtest ile birden fazla pencerede tekrar edilecek.
- Bu blokta iki büyük tuzak yaşandı, ikisi de ML.NET/EF Core'un "derlenir
  ama çalışmaz" türünden hataları: (1) `DateTime` kurulumunun SQL'e
  çevrilememesi, (2) `CreateTimeSeriesEngine`'in doğru namespace'ini bulmak
  için reflection gerekmesi. Reflection ile API doğrulama tekniği bu blokta
  da işe yaradı.
- Kullanıcının "hallüsinasyon mu?" sorusuyla yapılan kontrol önemliydi: ilk
  açıklamam (anomali pencereleri tahmini bozuyor) doğrulanmadan verilmişti;
  veri kontrolü son 30 saatin temiz olduğunu gösterdi, açıklama
  "hipotez" olarak yeniden çerçevelendi. Bu tür açıklamaların veriyle
  doğrulanması gerektiği bir ders oldu.

### Ek: Rolling-origin backtest ve eğitim verisi temizliği (2026-10-03)

**Yapılanlar**:
- Tek 24 saatlik holdout yerine 5 kesim noktası (her biri 24 saat geriye)
  ile rolling-origin backtest. Her pencerede eğitim verisi kesim noktasından
  önceki veriyle sınırlı (sızıntı yok), tahmin ufku 24 saat.
- Pencere başına gerçek/tahmin tablosu yazdırıldı (hata ayıklama için).
- Her pencerede eğitim verisindeki sıçramalar, o pencerenin medyanının 3
  katını aşan saatler olarak tanımlanıp medyanla değiştirildi. Silme değil
  değiştirme seçildi, çünkü zaman serisinin düzenli adımları korunmalı.
  Temizlik yalnızca eğitim verisine uygulandı, test (gerçek) değerleri
  olduğu gibi kaldı.

**Denemeler** (her biri tek değişken, öncekine göre):
1. Baseline (window 24, eğitim 168 saat): tek pencere MAPE %17,27. Backtest
   ortalaması (5 pencere) %59,84, dağılım 16,7% ile 110,7% arası. Yani tek
   pencere sonucu güvenilir değilmiş.
2. Window 48 (diğer her şey aynı): tek pencere MAPE %106,79. Geri alındı.
3. Window 168 + eğitim 672 saat: backtest ortalaması **%46,22** (16,7 → 13,4
   hafta içi). Haftalık örüntü kısmen öğrenildi, ama hafta sonu pencereleri
   ve sıçramalar hâlâ büyük hata veriyordu.
4. Eğitimdeki sıçramaların temizlenmesi (window 168, eğitim 672, eşik 3×
   medyan): backtest ortalaması **%8,72**. Pencereler: %4,73 / %9,57 /
   %19,98 / %5,55 / %3,78.

**Doğrulama sonucu**: Son yapılandırmada 5 pencerenin dördü %10 altında.
Pencere 3'ün (%20) hatası test döneminde 27 Eylül'deki gerçek sıçramadan
kaynaklanıyor (gerçek değer 140'a çıkıyor). Model bu sıçramayı öngöremez;
bu, anomali tespitinin (Blok 7-8) konusu.

**Review notları**:
- Önceki %17,27'lik baseline tek bir pencereye dayanıyordu ve bu pencere
  şanslıymış. Tek holdout ile model seçmek yanıltıcı olurdu. Backtest bunu
  ortaya çıkardı.
- Hipotez zinciri veriyle sınandı: (a) eğitimdeki sıçramalar tahmini bozar
  → temizlik sonrası iyileşme doğruladı. (b) Haftalık örüntü öğrenilmesi
  gerekir → window 168 iyileştirdi. (c) Test'teki sıçramalar öngörülemez →
  pencere 3 hatası bunu gösteriyor.
- **Dürüstlük notu**: 3× medyan eşiği elle seçildi ve yalnızca bu veri
  setinde denendi. Farklı eşiklerle sonuç değişebilir; bu, genelleme iddiası
  için yeterli değil. Blok 5'te eşik duyarlılığı test edilmeli.
- Tahmin, gerçek 24 saat değerleri yan yana (pencere 4 örneği) incelendiğinde
  hafta sonu gece düşüşünün (~9 vs ~18) modelin hafta içi ritmiyle
  karıştığı görüldü. Window 168 bunu büyük ölçüde çözdü.

### Ek: Sıçrama eşiği duyarlılık testi (2026-10-03)

Eşik (medyanın kaç katı) 2×, 3×, 4×, 6× için aynı 5 pencereli backtest:

| Eşik | P1 | P2 | P3 | P4 | P5 | Ortalama |
|---|---|---|---|---|---|---|
| 2× | 4,73 | 9,57 | 19,98 | 5,55 | 3,78 | 8,72 |
| 3× | 4,73 | 9,57 | 19,98 | 5,55 | 3,78 | 8,72 |
| 4× | 11,02 | 14,71 | 19,98 | 5,55 | 3,78 | 11,01 |
| 6× | 11,84 | 16,50 | 19,98 | 5,55 | 3,78 | 11,53 |

Okuma: 2-3× aralığında sonuç değişmiyor (plato), 4× ve üstünde pencere 1-2
bozuluyor (temizlenmeyen sıçramalar eğitime giriyor). 3× platonun içinde,
seçim makul. Alt sınır (normal gündüz tepesi ≈ 1,6× medyan) test edilmedi.

---

## Blok 7 — Anomali tespiti: IidSpike, ground truth persistance, precision/recall

- **Yap**: `IidSpikeEstimator` ile saatlik maliyet serisinde anomali tespiti;
  Blok 2'deki `anomalyWindows`'u (daha önce hiç kalıcı olmayan) DB'ye
  taşımak; tespit edilenleri ground truth'a karşı precision/recall ile ölçmek.
- **Neden**: Forecasting "normalde ne olur" sorusuna cevap veriyordu, anomali
  tespiti "şu an normalden sapma var mı" sorusuna cevap veriyor — FinOps
  Copilot'un maliyet sıçramasını **aynı gün yakalama** iddiasının temeli bu.
- **Doğrulama**: Precision/recall sayıları, bilinen (DB'ye kaydedilmiş) 6
  pencereye karşı hesaplanmalı.
- **Claude'a gel**: API keşfi (`mlContext.Transforms` vs `mlContext.Forecasting`,
  `confidence` parametresinin deprecated int/double overload'ları),
  precision/recall tanımı (pencere bazlı mı saat bazlı mı) kararı.

**Durum**: ✅ Review edildi (2026-10-17)

**Yapılanlar**:
- İki oturumda tamamlandı: Cuma (17 Ekim) bonus 1 saatte pipeline kuruldu ve
  ilk ham sıçrama listesi alındı (henüz ground truth karşılaştırması yoktu);
  Cumartesi aynı gün devamında ground truth kalıcı hale getirildi ve
  precision/recall hesaplandı.
- API hataları, Blok 1'deki reflection bulgularına rağmen ilk denemede
  birkaç kez yanlış kuruldu: `mlContext.Forecasting.DetectIidSpike(...)`
  yazıldı (olmayan bir üye — asıl yer `mlContext.Transforms`), ayrıca
  `trainingWindowSize` diye var olmayan bir parametre eklendi (o parametre
  `DetectSpikeBySsa`'ya ait, `DetectIidSpike`'a değil). Derleyici hataları
  sırayla düzeltildi. Ayrıca `confidence: 95` (int) overload'ı deprecated
  çıktı, `confidence: 95.0` (double) olarak değiştirildi.
- `allInputs`/`allData`: `hourly`'nin **tamamı** (backtest dilimleri değil)
  `CostInput`'a çevrilip `IDataView`'a yüklendi — anomali tespiti tüm
  geçmişe bakmalı, sadece bir pencereye değil.
- `finalPipeline.Fit(allData).Transform(allData)` sonrası
  `CreateEnumerable<SpikeResult>(..., reuseRowObject: false)` ile sonuç
  tekrar C# listesine çevrildi. `SpikeResult.Prediction` 3 elemanlı bir dizi
  (anomali bayrağı, p-değeri, martingale skoru).
- `spikePoints`: iki aşamalı `Select` — önce `(result, index)` overload'ı ile
  her tahmine sıra numarası eklendi (çünkü `SpikeResult`'ta `Timestamp` yok),
  `Where` ile `Prediction[0] == 1` olanlar filtrelendi, sonra `index` ile
  `hourly`'den gerçek zaman damgası geri çekildi.
- **Ground truth kalıcılığı**: `AnomalyWindow` entity'si (Domain), `LlmDbContext`'e
  `DbSet<AnomalyWindow>` eklendi, migration (`AddAnomalyWindows`) oluşturulup
  uygulandı. `DataGenerator`'a, ürettiği `anomalyWindows` tuple listesini bu
  tabloya yazan kod eklendi. Mevcut 429K/440K satırlık tablo (ground truth'u
  hiç içermediği için) `TRUNCATE` edilip veri baştan üretildi — bu sefer
  pencereler de kalıcı.
- **Precision/recall tanımı bilinçli bir karar**: recall **pencere bazlı**
  (bir pencerenin en az bir saati yakalandıysa "bulundu" sayılır) tanımlandı,
  saat bazlı değil — çünkü iş hedefi "bu olay oldu mu, fark ettik mi"
  sorusu, "olayın her saatini tek tek yakaladık mı" değil.

**Doğrulama sonucu**: 429.193 log, 6 anomali penceresi (kalıcı). Model 14
saatlik sıçrama tespit etti. **Precision %57,1** (8/14 doğru), **Recall
%83,3** (5/6 pencere en az bir saatiyle yakalandı).

**Review notları**:
- Recall iyi, precision orta. Yanlış pozitiflerin (6 tanesi) büyük kısmı
  muhtemelen gerçek pencerelerin hemen öncesi/sonrasındaki geçiş saatleri —
  `IidSpikeEstimator` sıçramaya giriş/çıkışı da "anormal" sayıyor, ama ground
  truth penceresi anomalinin tam süresiyle sınırlı. Bu, pencere
  tanımının biraz dar olabileceğini düşündürüyor, test edilmedi.
- **Ground truth'un hiç kalıcı olmaması** (29 Eylül'den beri) önemli bir
  boşluktu — bu blok olmasaydı anomali tespitinin doğruluğu hiçbir zaman
  ölçülemezdi, sadece "bir şeyler buluyor" denebilirdi. Bu, "doğrulanabilir
  olmayan bir iddia kanıt değildir" prensibinin somut bir örneği.
- İki günlük (Cuma bonus + Cumartesi planlı) bir blok oldu — tahmin ettiğimiz
  "4-6 oturum daha" takviminin bir adımı daha tamamlandı.

---

## Blok 8-9 (kısmi) — Embedding + K-Means kümeleme

- **Yap**: `LlmFinOpsCopilot.Clustering` projesi; benzersiz prompt'ları
  Ollama (`nomic-embed-text`) ile embedding'e çevirmek; ML.NET K-Means ile
  benzer prompt'ları kümelemek — "hangi istekler tekrar tekrar soruluyor"
  (cache fırsatı) sorusunun temeli.
- **Neden**: Forecasting "ne kadar harcanıyor", anomali tespiti "ne zaman
  anormal" sorularına cevap veriyordu; kümeleme "neden/nereye harcanıyor,
  tekrar eden ne var" sorusuna cevap veriyor — üçü birlikte guarded LLM
  katmanının referans alacağı üç kanıt kaynağı.
- **Doğrulama**: Küme sayıları makul dağılmalı (tek bir dev kümeye
  toplanmamalı), embedding boyutu modelin beklenen boyutuyla (768) eşleşmeli.
- **Claude'a gel**: Aspire'sız Ollama bağlantısı kurmak (Proje 1'den farklı,
  doğrudan container), doğru NuGet paketini bulmak (`Microsoft.Extensions.
  AI.Ollama` diye bir paket yokmuş, gerçek paket `OllamaSharp`), K-Means
  API'sini doğrulamak.

**Durum**: 🔶 Kısmi tamamlandı (2026-10-19) — çekirdek pipeline (embedding +
kümeleme) çalışıyor ve doğrulandı; her kümedeki örnek prompt'ları yazdırma
(kümeleme sonucunu somutlaştıran küçük bir ek) Çarşamba'ya (guarded LLM
bloğuyla aynı oturuma) bırakıldı.

**Yapılanlar**:
- Ayrı bir Ollama container'ı (`llmfinops-ollama`, port 11435, Proje 1'den
  bağımsız) Docker ile başlatıldı, `nomic-embed-text` modeli indirildi.
- Madde 0: `Microsoft.Extensions.AI.Ollama` adında bir paket **yokmuş**
  (tahmin yanlış çıktı, baştan doğru paket arandı) — gerçek paket
  `OllamaSharp` (5.5.0), `OllamaApiClient` sınıfı doğrudan
  `IEmbeddingGenerator<string, Embedding<float>>` arayüzünü uyguluyor
  (reflection ile doğrulandı), Aspire'a ihtiyaç yok.
- `mlContext.Clustering.Trainers.KMeans(...)`'in gerçek imzası da reflection
  ile doğrulandı (`ClusteringTrainers` katalogu üzerinden, `featureColumnName`
  + `numberOfClusters`).
- Kullanıcı, kodu tek seferde (ara adım adım değil) yazdı — sonucunda
  birikmiş çok sayıda hata çıktı: yanlış DbContext sınıf adı
  (`FinOpsDbContext`), kırık ifade zinciri (`;` sonra `.UseNpgsql`, bu
  projede tekrar eden bir hata deseni), yanlış connection string (port/DB
  adı), yazım hatası (`LlmCallogs`), var olmayan bir sınıf
  (`OllamaEmbeddingGenerator`) ve yanlış model adı (embedding yerine sohbet
  modeli `llama2-7b`), var olmayan bir metod adı (`GenerateEmbeddingAsync`
  yerine `GenerateVectorAsync`), ve prompt bilgisini kaybeden bir veri yapısı.
  Hepsi tek seferde, Claude tarafından yeniden yazılıp build+run ile
  doğrulandı — kullanıcının isteği üzerine (birikmiş hata sayısı fazlaydı,
  tek tek düzeltmek yerine düzeltilmiş hali görüp farkı incelemeyi tercih
  etti).
- K-Means pipeline'ına kullanıcı kendi inisiyatifiyle `NormalizeMinMax` ve
  (gereksiz olan, sonradan kaldırılan) `MapValueToKey("Prompt")` adımlarını
  eklemişti — `NormalizeMinMax` makul bir ek (embedding vektörlerini
  kümelemeden önce normalize etmek iyi bir pratik), `MapValueToKey` kümeleme
  bağlamında anlamsızdı, kaldırıldı.

**Doğrulama sonucu**: 99 benzersiz prompt bulundu (429K satırlık veride),
her biri 768 boyutlu embedding'e çevrildi, K=10 ile kümelendiğinde dağılım
4-23 prompt/küme arası — tek bir dev kümeye toplanmadı (sentetik
prompt'ların ``"This is a sample prompt {N}"`` kalıbına rağmen bir miktar
ayrışma yakalanmış).

**Review notları**:
- Kullanıcının "çok fazla hazır fonksiyon var" gözlemi üzerine
  `LoadFromEnumerable`/`CreateEnumerable` çiftinin bu projede **üçüncü** kez
  (forecasting, anomali tespiti, şimdi kümeleme) aynı şekilde kullanıldığı
  gösterildi — yeni bir kavram değil, aynı iki kavramın tekrarı; bu,
  "ML.NET geniş ama senin öğrendiğin çekirdek örüntü sınırlı" mesajını
  somutlaştırdı.
- Kullanıcı bu blokta "kod yazımını tam içselleştirmedim ama kavramlar
  (embedding, kümeleme mantığı, ML.NET girdi/çıktı köprüsü) pekişti" diye
  kendi değerlendirmesini yaptı — bu, dürüst ve doğru bir öz-değerlendirme;
  tekrarla sözdiziminin de oturacağı öngörülüyor.
- Sentetik veri sınırlaması burada da görüldü (Blok 2/4'teki gibi): gerçek
  dünya prompt çeşitliliği olmadığı için kümeleme sonucu "ilginç" olsa da
  iş değeri sınırlı — bu proje bir metodoloji/pipeline kanıtı, gerçek bir
  üretim analizi değil, README'de bu netlik korunmalı.

---

## Blok 10-12 — Guarded LLM: typed structured output + grounding-check

- **Yap**: `LlmFinOpsCopilot.Analyst` projesi; üç modülün (forecasting,
  anomali, kümeleme) sonuçlarını temsil eden üç basit kanıt sorgusu; bu
  kanıtları `IChatClient` ile bir LLM'e sunup typed öneri üretmesini
  isteme; LLM'in verdiği `EvidenceValue`'yu gerçek kanıtla karşılaştırıp
  kanıtsız/uydurma değerleri reddeden bir grounding-check.
- **Neden**: Bu, projenin asıl farklılaştırıcı iddiası — "ChatGPT'ye sor"
  seviyesinden ayıran şey, LLM'in **kanıtsız konuşamaması**.
- **Doğrulama**: Hem geçen (grounded) hem reddedilen (halüsinasyon)
  senaryoyu canlı görmek.
- **Claude'a gel**: Yanlış API kullanımı (var olmayan bir SDK'nın
  tipleri), ham yanıtı inceleyip neden boş/eksik geldiğini teşhis etmek,
  prompt mühendisliği (sayısal karışıklık sorunu).

**Durum**: ✅ Review edildi (2026-10-24)

**Yapılanlar**:
- Üç kanıt sorgusu (Forecasting/anomali/kümeleme modüllerini yeniden
  çalıştırmadan, onların temsilini basit LINQ sorgularıyla üreten):
  trend yüzdesi (son 7 gün vs önceki 7 gün maliyet, `latestTimestamp`
  verideki en son zaman olarak alınarak — gerçek `DateTime.UtcNow` ile
  karşılaştırmak boş aralıklara yol açardı), `AnomalyWindows` tablosundaki
  toplam pencere sayısı (Blok 7'de kalıcı hale getirdiğimiz veri burada
  işe yaradı), en sık tekrar eden prompt (`GroupBy`+`Count`+
  `OrderByDescending`).
- `AnalystRecommendation` typed sonuç sınıfı (`Recommendation`,
  `EvidenceType`, `EvidenceValue`) — Proje 1'deki
  `ProductValidationResult` deseninin tekrarı.
- İlk denemede kullanıcı, projeye hiç eklenmemiş bir SDK'nın API'sini
  (`GetChatCompletionsAsync`, `ChatCompletionsOptions`, `ChatMessage`,
  `ChatRole` — muhtemelen Azure OpenAI SDK'sından akılda kalan bir örnek)
  yazdı; derlenmedi. Doğru API (`GetResponseAsync<T>`, Proje 1'den
  tanıdık) ile değiştirildi.
- İlk çalıştırmada sonuç boş geldi (`Recommendation`/`EvidenceType` boş,
  `EvidenceValue: 0`) — tahmin yürütmeden `response.Text` ile **ham
  yanıtı** incelendi: model yalnızca `{"evidenceValue": 4481}` üretmiş,
  diğer iki alanı atlamış. Prompt'a üç alanın da **zorunlu** olduğu
  açıkça yazılınca (alan adları tek tek belirtilerek) model üçünü de
  doldurmaya başladı.
- **Grounding-check**: `EvidenceType`'a göre `switch` ifadesiyle,
  `EvidenceValue`'nun ilgili gerçek sayıya (küçük bir tolerans payıyla,
  `< 0.5`) eşit olup olmadığı kontrol edildi. Eşit değilse "REDDEDİLDİ"
  mesajı veriliyor.
- **Tutarlı bir halüsinasyon bulgusu**: Model, "Trend" kanıtını seçtiğinde
  defalarca `evidenceValue: 7` döndürdü — gerçek değer %0,91 iken.
  Sebep teşhis edildi: prompt'ta "**Son 7 günde**... önceki **7** güne
  göre" ifadesi geçiyordu, model muhtemelen bu **7** sayısına
  takılıp trend yüzdesi yerine onu yazıyordu. "7 gün" ifadeleri "geçen
  hafta"/"önceki hafta" ile değiştirilince (sayısal karışıklığı ortadan
  kaldırarak) model doğru değeri (`0.91`) üretmeye başladı.

**Doğrulama sonucu**: Prompt düzeltmesinden önce iki ayrı çalıştırmada
tutarlı şekilde `GROUNDING-CHECK: BAŞARISIZ` (model `evidenceValue: 7`
uydurdu, gerçek değer 0,91 idi) — mekanizma bunu doğru reddetti. Düzeltme
sonrası `GROUNDING-CHECK: GEÇTİ` (`evidenceValue: 0.91`, gerçek trend
değeriyle birebir eşleşti).

**Review notları**:
- Bu blok, projenin iddiasını **canlı olarak** kanıtladı: sistem sadece
  "bazen doğru çalışıyor" değil, "yanlış olduğunda bunu tespit edip
  reddediyor" — ikisini de gözlemledik, ikisi de doğru davrandı.
- Halüsinasyonun kök sebebi (prompt'taki tekrar eden "7" sayısı) ilginç
  bir ders: LLM'e kanıt sunarken, kanıtın **kendisiyle çakışabilecek**
  başka sayılar (burada zaman penceresinin uzunluğu) prompt'ta
  bulunmamalı — model bunları birbirine karıştırabiliyor. Bu, prompt
  mühendisliğinde "alakasız sayısal gürültüyü azalt" diye genellenebilecek
  bir prensip.
- Küçük model (3B) davranışı tutarsız — bazen tüm alanları dolduruyor
  bazen atlıyor, aynı prompt'la bile. Bu, Proje 1'in risk maddelerinden
  birinin (küçük modellerde bozuk/eksik yapılandırılmış çıktı riski) somut
  bir doğrulanması.
- Grounding-check'in toleransı (`< 0.5`) elle seçildi, farklı toleranslarla
  test edilmedi — küçük bir dürüstlük notu, ileride istenirse
  genişletilebilir.

---

## Roller

- **Kullanıcı**: Her bloğun kodunu bizzat yazar. Takıldığında spesifik soru
  sorar — Claude tam çözümü direkt yazmaz, önce neyin yanlış olduğunu açıklar.
- **Claude**: Mimarı belirledi, her blok sonunda review yapar, config/
  boilerplate işlerinde (Directory.Build.props vb.) ve Madde 0 risk
  doğrulamasında doğrudan yardımcı olur.
- **Git**: Her blok grubunun sonunda (genelde haftalık, bir önceki projede
  olduğu gibi) küçük, anlamlı bir commit.
