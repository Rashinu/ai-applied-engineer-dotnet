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

## Roller

- **Kullanıcı**: Her bloğun kodunu bizzat yazar. Takıldığında spesifik soru
  sorar — Claude tam çözümü direkt yazmaz, önce neyin yanlış olduğunu açıklar.
- **Claude**: Mimarı belirledi, her blok sonunda review yapar, config/
  boilerplate işlerinde (Directory.Build.props vb.) ve Madde 0 risk
  doğrulamasında doğrudan yardımcı olur.
- **Git**: Her blok grubunun sonunda (genelde haftalık, bir önceki projede
  olduğu gibi) küçük, anlamlı bir commit.
