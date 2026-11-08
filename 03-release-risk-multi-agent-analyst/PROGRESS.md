# İlerleme Günlüğü — Release-Risk Çok-Ajanlı Analist

Kural: Bir fazın **Durum**'u ✅ Review edildi olmadan bir sonraki faza
geçilmez. "Yapılanlar / Doğrulama" kısmını sen doldurursun, "Review
notları" kısmını ben.

**Kaynak**: Kullanıcının kendi haftalık "Applied AI & Data Engineer Proje
Radarı"ndaki **Proje C** (18 Ağustos 2026). Proje 1 (Marketplace AI
Regulation Agent) ve Proje 2 (LLM FinOps Copiloti) bittikten sonra, 28
Ekim 2026'da radar PDF'i paylaşıldı ve bu proje seçildi — gerekçe:
radar'daki 8 fikir (H, I, J, L, M, N, P, Q, R) Microsoft Agent Framework
ve/veya MCP üzerine kurulu, bu iki kavrama bu projede ilk kez dokunuyoruz;
burayı öğrenmek radar'ın yarısından fazlasının önünü açıyor.

**Faz 2'nin kararı**: Önceki projede (LLM FinOps Copiloti) "Faz 2" dashboard/
CI anlamına geliyordu ve kapsam dışı bırakılmıştı. Bu projede "Faz" terimi
**farklı bir anlamda** kullanılıyor — bu projenin kendi gelişim aşamaları
(Faz 1-8), önceki projenin Faz 1/Faz 2 ayrımıyla karıştırılmamalı.

---

## Mimari (referans — tüm fazlar boyunca sabit)

```
Sentetik veri (Postgres)
  ReleaseSignal            — bir release kaydı: değişen satır sayısı,
                              commit sayısı, reviewer sayısı, test coverage,
                              başarısız CI run sayısı
  ServiceIncidentHistory   — bir servisin geçmiş olay kaydı (severity ile)

MCP Sunucusu — ajanların doğrudan DB'ye değil, tanımlı ARAÇLAR üzerinden
  veriye erişmesini sağlıyor:
    get_release_signals(releaseId)
    get_service_incident_history(serviceName)

Microsoft Agent Framework orkestrasyonu — 3 uzman ajan + 1 orkestratör:
  "Kod Risk Ajanı"     → değişim büyüklüğü/karmaşıklığına bakar
  "Test Risk Ajanı"    → coverage/CI sinyallerine bakar
  "Geçmiş Risk Ajanı"  → bu servisin incident geçmişine bakar
  Orkestratör          → üç ajanın bulgularını TOPLAYIP tek bir risk kararı
                          verir — kanıta dayalı (Proje 2'deki grounding
                          disiplininin devamı): karar, hangi ajanın hangi
                          bulgusuna dayandığını belirtmek zorunda

Minimal API — tek, birleşik endpoint (Proje 2'deki GET /insights deseni)
```

## Bilinen Riskler / Madde 0'da Doğrulananlar

Faz 1'de doğrulandı (gerçek NuGet araması + throwaway probe projesiyle
restore/build denemesi — Proje 1/2'de öğrenilen teknik):
- `Microsoft.Agents.AI` 1.26.0, `Microsoft.Agents.AI.OpenAI` 1.26.0,
  `ModelContextProtocol` 2.3.0 — hepsi gerçek, nuget.org'da mevcut, radar
  PDF'indeki versiyon numaralarıyla birebir eşleşiyor.
- Üçü birlikte `net10.0` hedefiyle sorunsuz restore + build oluyor.

Doğrulanmamış, ileride dikkat edilecek:
- Bu API'lerin gerçek kullanım şekli (Agent Framework'te ajan tanımlama,
  MCP sunucusu kurma sözdizimi) henüz hiç denenmedi — Faz 3-4'te reflection
  ile keşfedilecek.
- `Microsoft.Agents.AI` 1.22-1.26 arası birkaç **breaking change** geçirmiş
  (radar PDF'inde not edilmiş: `AgentSessionStore`, MCP 2.x uyumluluk
  temeli vb.) — dokümantasyon güncel olmayabilir, API'yi doğrulamadan
  tahmin etmeyeceğiz.

---

## Taslak Faz Planı

| Faz | İçerik | Durum |
|---|---|---|
| 1 | Proje iskeleti + Madde 0 + Domain entity'leri (`ReleaseSignal`, `ServiceIncidentHistory`) | ✅ |
| 2 | Infrastructure (DbContext, migration) + sentetik veri üretici | ✅ |
| 3 | MCP sunucusu: `get_release_signals`, `get_service_incident_history` araçları | ✅ |
| 4 | Agent Framework'e giriş: tek ajan (Kod Risk Ajanı) + MCP aracı çağırma | ✅ |
| 5 | Kalan iki uzman ajan (Test Risk, Geçmiş Risk) + Faz 4'ten kalan 4 kalite sorunu | ⬜ |
| 6 | Orkestratör ajan: üç bulguyu toplayıp kanıta dayalı karar | ⬜ |
| 7 | Minimal API ile dışa açmak | ⬜ |
| 8 | Kapanış: README + LinkedIn | ⬜ |

Not: Bu iskelet kesin değil — Proje 1/2'de olduğu gibi ilerledikçe revize
edilir.

---

## Faz 1 — Proje iskeleti + Madde 0 + Domain entity'leri

- **Yap**: Proje klasörü, `global.json`, `.slnx`, `Directory.Build.props`,
  `Directory.Packages.props`, `ReleaseRiskAnalyst.Domain` class library,
  içinde `ReleaseSignal` ve `ServiceIncidentHistory` entity'leri. Madde 0:
  Agent Framework ve MCP C# SDK paketlerinin .NET 10 ile gerçekten uyumlu
  olduğunu doğrula.
- **Neden**: Paket/versiyon riskini en başta görmek, radar PDF'inin
  kendisinin bile "breaking change" uyarıları içerdiği bir ekosistemde.
- **Doğrulama**: `dotnet build` 0 hata/0 uyarı.
- **Claude'a gel**: Entity'lerin alan listesiyle örtüştüğünü, namespace/
  nullable hatalarını review et.

**Durum**: ✅ Review edildi (2026-10-28)

**Yapılanlar**:
- Proje adı: `03-release-risk-multi-agent-analyst`, çözüm adı
  `ReleaseRiskAnalyst`.
- Madde 0: `dotnet package search` ile üç paketin gerçekten var olduğu
  doğrulandı (radar PDF'indeki versiyon numaralarıyla eşleşiyor), throwaway
  bir probe projesinde `net10.0` ile restore + build denendi, 0 hata.
- İki entity, kullanıcı tarafından birkaç review turunda yazıldı:
  1. İlk taslakta namespace hatası tekrar etti — her iki dosyada da
     (`ServiceIncidentHistory.Domain`, `ReleaseSignal.Domain`) namespace,
     proje adı yerine **sınıfın kendi adıyla** yazılmıştı. Proje 1/2'de
     görülmemiş yeni bir hata deseni; muhtemelen IDE'nin otomatik
     namespace önerisinin sınıf adını kopyalamasından kaynaklandı.
  2. `string` alanlara `= string.Empty;` varsayılanı unutulmuştu (CS8618
     riski) — iki dosyada da ayrı ayrı hatırlatıldı, düzeltildi.
  3. `ServiceIncidentHistory`'deki enum, Proje 1'den kalma yanlış bir adla
     (`ProductStatus`) ve **sınıfın içine gömülü** olarak yazılmıştı,
     ayrıca hiçbir property'ye bağlı değildi (tanımlı ama kullanılmıyordu).
     `IncidentSeverity` olarak yeniden adlandırıldı, sınıfın dışına
     (bağımsız tip olarak) çıkarıldı, `Severity` property'si eklendi.
  4. Kullanıcı enum kavramını (sabit, isimlendirilmiş değer kümesi — dizi
     değil) ve enum ile "olası değerleri tutan bir dizi" (örn.
     `severities[]`, rastgele seçim için) arasındaki farkı sorguladı —
     ikisinin farklı amaçlara hizmet ettiği (biri tip tanımı, diğeri veri
     üretirken seçim aracı) somut örnekle açıklandı.

**Doğrulama sonucu**: `dotnet build` (Domain projesi) — 0 hata, 0 uyarı.

**Review notları**:
- Namespace hatasının **iki ayrı dosyada tekrar etmesi**, bunun tek
  seferlik bir yanlışlık değil muhtemelen bir alışkanlık/IDE davranışı
  olduğunu gösteriyor — kullanıcıya gelecek dosyalarda bu noktaya özellikle
  dikkat etmesi hatırlatıldı.
- Enum'ın nereye, nasıl tanımlanacağı konusundaki kafa karışıklığı
  (sınıfın içi/dışı, isimlendirme, kullanım) Proje 1'deki `ProductStatus`
  örneğine geri dönülerek çözüldü — önceki projelerdeki somut örneklere
  referans vermek hâlâ etkili bir öğretim yöntemi.

---

## Faz 2 — DbContext + migration + sentetik veri üretici

- **Yap**: `ReleaseRiskAnalyst.Infrastructure` projesi, `ReleaseRiskDbContext`,
  design-time factory, ilk migration, ayrı bir Postgres container'ı.
- **Durum**: ✅ Review edildi (2026-10-31). DbContext/migration kısmı
  Claude tarafından doğrudan yapıldı (mekanik, üçüncü kez tekrarlanan
  desen, düşük öğrenme değeri — Proje 1/2'deki aynı istisna). Sentetik veri
  üretici mantığı (asıl düşünce gerektiren kısım) **kullanıcı tarafından**
  yazıldı, 28 Ekim'de ertelenmişti, bu oturumda bitirildi.

**Yapılanlar**:
- `ReleaseRiskDbContext : DbContext` — `DbSet<ReleaseSignal>`,
  `DbSet<ServiceIncidentHistory>`.
- **Gerçek bir EF Core hatası çıktı ve çözüldü**: `dotnet ef migrations add`
  ilk denemede "ReleaseSignal birincil anahtar gerektiriyor" hatası verdi.
  Sebep: EF Core, `Id` ya da `<SınıfAdı>Id` adlı alanları otomatik birincil
  anahtar sayıyor; entity'lerdeki `SignalId`/`IncidentId` alan adları bu
  kalıba uymuyordu (Proje 1/2'de hep `Id` kullanılmıştı, bu ilk kez farklı
  bir isimlendirme). Çözüm: `OnModelCreating` içinde
  `modelBuilder.Entity<T>().HasKey(x => x.AlanAdı)` ile açıkça belirtildi,
  entity'lerin kendi alan adlarına dokunulmadı.
- Ayrı bir Postgres container'ı (`releaserisk-postgres`, port 5434 — Proje
  1'in ve Proje 2'nin (`llmfinops-postgres`, 5433) container'larından
  bağımsız), `unless-stopped` restart policy'siyle.
- Migration oluşturuldu ve uygulandı, iki tablo (`ReleaseSignals`,
  `ServiceIncidentHistories`) Postgres'te doğrulandı.

**Review notları**:
- `SignalId`/`IncidentId` gibi `Id`'den farklı isimlendirmeler EF Core'un
  otomatik anahtar bulma kuralını bozabiliyor — bu, önceki iki projede hiç
  karşılaşılmamış, bu projeye özgü (kullanıcının kendi isimlendirme
  tercihinden kaynaklanan) yeni bir ders. Gelecekte `Id` dışı bir isim
  seçerse, `HasKey` ile açıkça belirtmesi gerektiğini bilecek.

### Sentetik veri üretici (31 Ekim 2026)

**Yapılanlar**:
- `ReleaseRiskAnalyst.DataGenerator` konsol projesi.
- **Risk skoru fonksiyonu** (`CalculateReleaseRisk`) — kullanıcı tarafından
  **ilk denemede hatasız** yazıldı: dört sinyali (`linesChanged`,
  `reviewerCount`, `failedCiRunCount`, `testCoverage`) 0-1 aralığına
  normalize edip (tavan/clamp ile), yön farkını (`1 - normalized` ile
  "arttıkça azaltan" sinyaller için), ağırıklı toplamla (0.4/0.3/0.2/0.1,
  toplamı 1) tek bir risk skoruna indirgiyor. Bu, klasik bir "ağırlıklı
  toplam skorlama" (weighted sum scoring) algoritması — Proje 2'deki yeni
  kavramların aksine (ki hepsi birkaç hata turu gerektirmişti), bu sefer
  hiç hata çıkmadı. Önceki iki projede kavramsal bir yeniliğin ilk
  denemede hatasız yazılması **ilk kez** oldu.
- Üretim döngüsünde (kullanıcı yazdı, Claude review etti ve düzeltti)
  dört gerçek hata çıktı, hepsi mekanik/sözdizimsel (kavramsal değil):
  1. Eksik `using ReleaseRiskAnalyst.Domain;`.
  2. `new ReleaseSignal{}` sonunda eksik noktalı virgül, **ve** nesne hiç
     doldurulmamış.
  3. `release.Add(release)` — tekil nesne (`release`) üzerinde `.Add()`
     çağrılmaya çalışılmış, doğrusu `releases.Add(release)` (çoğul liste).
  4. **En kritik hata**: `release` nesnesi boş oluşturulduktan sonra,
     `CalculateReleaseRisk`'e **tamamen ayrı, yeni üretilmiş rastgele
     sayılar** veriliyordu — yani risk skoru, `release`'in kendi
     alanlarından değil, ondan kopuk bir "paralel gerçeklik"ten
     hesaplanıyordu. Çözüm: `release` tüm alanlarıyla (object initializer
     ile, tek seferde) dolduruldu, risk skoru hesaplanırken
     `release.LinesChanged` gibi **o nesnenin kendi alanları** kullanıldı.
  5. `Severity = Random.Shared.Next(1, 6)` — enum'a (4 değerli,
     `IncidentSeverity`) doğrudan 1-5 arası bir `int` atanmaya çalışılmış
     (tip uyumsuz + aralık dışı). `severities[]` dizisinden rastgele seçim
     yapacak şekilde düzeltildi.
- DB'ye yazma kısmını kullanıcı kendi başına, Proje 2'deki
  `DbContextOptionsBuilder` → `using var db` → `AddRangeAsync` →
  `SaveChangesAsync` desenini tekrarlayarak, **hatasız** yazdı. Kendi
  inisiyatifiyle `await db.Database.EnsureCreatedAsync();` eklendi —
  migration'larla birlikte kullanılması EF Core'da genelde önerilmeyen bir
  karışım (zararsız şu an çünkü tablolar zaten migration'la var, ama
  gelecekte yeni bir migration eklenirse gözden geçirilmeli).

**Doğrulama sonucu**: `dotnet run` → `Toplam 300 release, 182 incident
üretildi.` / `Veriler veritabanına kaydedildi.` Postgres'te
`SELECT COUNT(*)` ile doğrulandı: 300 `ReleaseSignals`, 182
`ServiceIncidentHistories`. ~%54-61 incident oranı (farklı çalıştırmalarda
değişiyor, rastgelelik nedeniyle) — gerçekçi bir dağılımdan biraz yüksek,
muhtemelen `linesChanged`'in 0-2000 gibi geniş bir aralıktan rastgele
seçilmesi normalize edilince ortalamayı yukarı çekiyor. İnce ayar olarak
not edildi, bugün düzeltilmedi.

**Review notları**:
- Bu oturumun en dikkat çekici bulgusu: **yeni bir algoritmik kavramın ilk
  denemede hatasız yazılması.** Önceki tüm "yeni kavram" girişlerinde
  (forecasting, anomali tespiti, embedding, guarded LLM, MCP paket
  doğrulama vb.) en az bir-iki hata turu olmuştu. Risk skoru fonksiyonunda
  hiç olmadı — kalan hatalar hep "zaten bilinen" bir deseni (nesne
  doldurma, liste ekleme, enum kullanımı) yanlış uygulamaktan kaynaklandı,
  "anlamadım" değil "dikkatsizlik" kaynaklıydı. Bu ayrım, kullanıcıya
  "bugün hızlı bitti, yeterince çalışmadım" diye düşündüğü bir anda somut
  kanıt olarak gösterildi.
- Kullanıcı yine (28 Ekim'deki gibi) "neden bu kadar hızlı bitti, üstüne
  koyalım mı" diye sorguladı. Bu sefer net bir "hayır" cevabı verildi,
  gerekçeyle: Faz 2 doğal bir durma noktası, Faz 3 (MCP) tamamen yeni bir
  alan, "hızlı bitti" hissinin peşinden acele başlamak yerine ayrı, istekli
  bir oturuma bırakılmalı. Kullanıcı bunu kabul etti ve **Faz 3'ü özellikle
  yavaş ve detaylı ele almayı** kendi isteğiyle talep etti — bu, bir
  sonraki oturum için açık bir yönerge.

---

## Faz 3 — MCP sunucusu (6 Kasım 2026)

- **Yap**: `ReleaseRiskAnalyst.McpServer` konsol projesi; MCP C# SDK'sının
  gerçek kullanım şeklini (attribute'lar, host/DI kurulumu) reflection ile
  doğrula; `ReleaseRiskTools` sınıfında iki tool (`get_release_signals`,
  `get_service_incident_history`); MCP Inspector ile uçtan uca doğrulama.
- **Durum**: ✅ Review edildi (2026-11-06). Proje iskeleti (csproj, paket
  referansları, `.slnx` kaydı, `Program.cs`'teki DI/host kurulumu) Claude
  tarafından yapıldı (konfigürasyon, düşük öğrenme değeri — Proje 1/2/3'ün
  aynı istisnası). `ReleaseRiskTools.cs`'teki iki tool metodu **kullanıcı
  tarafından** yazıldı.

**Yapılanlar**:
- Madde 0 (MCP SDK'sının gerçek API şekli): `ModelContextProtocol` paketi
  hiç kullanılmamıştı, sadece restore edildiği biliniyordu. Tahmin etmek
  yerine throwaway bir probe projesinde reflection ile gerçek tipler
  incelendi: `[McpServerToolType]` (sınıf seviyesi), `[McpServerTool]`
  (metot seviyesi) attribute'ları `ModelContextProtocol.Core` paketinde
  bulundu; `IMcpServerBuilder.WithToolsFromAssembly()` /
  `WithStdioServerTransport()` extension metodları `ModelContextProtocol`
  paketinde doğrulandı.
- `Program.cs` (Claude yazdı): `Host.CreateApplicationBuilder` ile genel
  host kuruluyor, `AddDbContext<ReleaseRiskDbContext>` DI'a kaydediliyor,
  `AddMcpServer().WithToolsFromAssembly().WithStdioServerTransport()` ile
  sunucu ayağa kaldırılıyor. Önemli bir detay: stdio transport, protokol
  mesajlarını stdout üzerinden taşıdığı için varsayılan konsol logger'ı
  stderr'e yönlendirildi (`LogToStandardErrorThreshold = LogLevel.Trace`)
  — yoksa loglar protokolü bozardı.
- `ReleaseRiskTools.cs` (kullanıcı yazdı) — `GetReleaseSignals` ve
  `GetServiceIncidentHistories`, ikisi de `ReleaseRiskDbContext db`'yi
  (DI'dan enjekte edilen) ve `string serviceName`'i (ajandan/JSON
  şemasından gelen gerçek argüman) parametre alıyor, `Where` + `ToListAsync`
  ile filtreli veri döndürüyor. Yazım sürecinde üç hata turu oldu:
  1. `GetReleaseSignals` gövdesi ilk başta boştu (`// ???`) — derleme
     hatası (dönüş tipi olan bir metot her yolda değer döndürmek zorunda).
  2. `serviceName` normalizasyonu için önce `ToList().ForEach(c => ...
     Replace(c, '-'))` gibi karakter-karakter bir yaklaşım denendi — somut
     bir örnekle (`"payment-service"` → `"---------------"`) gösterildi ki
     bu, string'teki her karakteri sırayla sildiğinden string'i tamamen
     bozuyor. Kullanıcının asıl niyeti trim+lowercase'miş; `.Trim()` ve
     `.ToLowerInvariant()`'ın zaten tüm string'i tek çağrıda işleyen hazır
     metotlar olduğu, karakter bazlı döngüye hiç gerek olmadığı açıklandı.
  3. `serviceName.Trim.ToLowerInvariant()` — `Trim` bir metot, parantezsiz
     yazılınca metot grubuna işaret ediyor, üzerine zincirleme
     yapılamıyor; `Trim()` olarak düzeltildi.
- **MCP Inspector ile doğrulama** (`npx @modelcontextprotocol/inspector
  dotnet run --project ...`): sunucu stdio üzerinden bağlandı, her iki
  tool da `Tools (2)` listesinde göründü (SDK, metot adlarını otomatik
  `snake_case`'e çevirip JSON şemasına koyuyor: `GetReleaseSignals` →
  `get_release_signals`). `get_release_signals(serviceName:
  "payment-service")` ilk denemede doğru, gerçek Postgres verisini
  döndürdü. `get_service_incident_histories` ilk denemede **boş `[]`**
  döndü — hata sanıldı, ama Postgres'te `payment-service` için 37 incident
  olduğu doğrulanınca (bir hata değil, gerçek veri sorunu da değil)
  sorunun Inspector'daki `serviceName` input alanına hiç yazılmamış
  olması (gri placeholder metni gerçek değer sanılmış) olduğu anlaşıldı;
  alana gerçekten yazılınca tool doğru sonucu döndü.

**Doğrulama sonucu**: `dotnet build` (McpServer projesi) — 0 hata. MCP
Inspector'da her iki tool, gerçek `serviceName` parametresiyle çağrıldığında
gerçek, Postgres'teki senkron verilerle eşleşen sonuçlar döndü.

**Review notları**:
- Bu projede ilk kez, "sonuç yanlış görünüyor" durumunda önce veriye
  (Postgres'e doğrudan `psql` ile bakarak), sonra isteğin gerçek içeriğine
  (Inspector'ın Protocol panelinden gönderilen `arguments`'a bakarak)
  gitme sırası izlendi — kod her zaman şüpheli olmak zorunda değil; boş
  `[]` sonucu bu kez girdi tarafındaki bir eksiklikten kaynaklanıyordu.
- Kullanıcının MCP kavramını (tool/attribute/DI-parametre ayrımı) oturtması
  birkaç geçiş gerektirdi (API karşılaştırması, satır satır inceleme,
  Replace'in gerçek semantiği) — bu, Proje 3'ün ilk kez karşılaşılan yeni
  bir protokol/kütüphane olmasıyla tutarlı, Proje 1/2'deki ML.NET/embedding
  kavramlarının ilk oturumlarında da benzer çoklu-geçiş ihtiyacı olmuştu.

---

## Faz 4 — Agent Framework'e giriş: tek ajan + MCP aracı çağırma (8 Kasım 2026)

- **Yap**: `ReleaseRiskAnalyst.Agents` konsol projesi; `Microsoft.Agents.AI`'ın
  gerçek kullanım şeklini (ajan kurma, MCP client, tool bağlama, çalıştırma)
  reflection ile doğrula; tek bir ajan ("Kod Risk Ajanı") kurup MCP
  sunucusundaki tool'ları gerçekten çağırabildiğini kanıtla.
- **Durum**: ✅ Review edildi (2026-11-08). Proje iskeleti, MCP client
  bağlantısı, `IChatClient` kurulumu Claude tarafından yapıldı (konfigürasyon
  — Faz 1/2/3'ün aynı istisnası). `instructions` metni ve ajanı çalıştırma
  (session + `RunAsync`) kısmı **kullanıcı tarafından** yazıldı. Ayrı bir
  Ollama container'ı (`releaserisk-ollama`, port 11436) + `llama3.2:3b`
  modeli bu proje için sıfırdan kuruldu (Proje 2'nin `llmfinops-ollama`'sı
  kapanışta kaldırılmıştı).

**Yapılanlar**:
- Madde 0 (Agent Framework + MCP client API şekli, reflection ile, tahmin
  yok): `ChatClientAgent : AIAgent` (`Microsoft.Agents.AI`), ctor
  `(IChatClient, string instructions, string name, string description,
  IList<AITool> tools, ...)`. MCP tarafında `StdioClientTransport` +
  `McpClient.CreateAsync(transport)` ile sunucuya bağlanılıyor,
  `mcpClient.ListToolsAsync()` bir `McpClientTool` listesi dönüyor —
  **`McpClientTool`, `Microsoft.Extensions.AI.AIFunction`'ın alt sınıfı**,
  yani MCP'den gelen tool'lar dönüşümsüz doğrudan ajana veriliyor.
  `agent.RunAsync(string, AgentSession)` bir `AgentResponse` dönüyor,
  cevap `response.Text`'te.
- `Program.cs` (Claude yazdı): MCP sunucusuna stdio ile bağlanma, tool
  listesini çekme, `OllamaApiClient`'ı `IChatClient` olarak kurma.
- `instructions` + ajanı çalıştırma (kullanıcı yazdı) — süreçte bir hata ve
  bir API karışıklığı çıktı:
  1. `AgentMessage` diye var olmayan bir tipi (`ChatMessage` ile karışmış
     olabilir) kullanmaya çalıştı — reflection'da `RunAsync`'in `string`
     veya `ChatMessage` kabul ettiği, `AgentMessage` diye bir tip hiç
     olmadığı gösterilip en basit (`string`) overload'a yönlendirildi.
  2. İlk denemede hedef servis adı olarak gerçek olmayan `"ServiceA"`
     yazılmıştı (DataGenerator'ın ürettiği gerçek adlar `payment-service`
     vb.) — düzeltildi.
- **İki ayrı gerçek altyapı sorunu çıktı ve çözüldü** (Claude tarafından,
  altyapı/config niteliğinde):
  1. `OllamaApiClient`'ın varsayılan `HttpClient.Timeout`'u (100sn),
     ajan + tool-calling (MCP round-trip dahil) + CPU üzerinde yavaş
     inference (`llama3.2:3b`, ~6.3 token/sn, GPU yok) kombinasyonu için
     yetersiz kaldı — `TaskCanceledException`. Önce 5 dakikaya çıkarıldı
     (`new HttpClient { Timeout = TimeSpan.FromMinutes(5) }` ile
     `OllamaApiClient`'ın `HttpClient` alan overload'u kullanılarak), ama
     bu bile yetmedi.
  2. Kullanıcının prompt'a eklediği "cevabını kısa tut" talimatına model
     uymadı — Ollama container loglarından doğrulandı: model 1400+ token
     üretmeye devam edip yine 5 dakikalık timeout'u aştı. **Ders**: küçük
     (3B) modeller "kısa tut" gibi talimatlara güvenilir şekilde uymuyor;
     bunu prompt mühendisliğiyle değil, API'nin kendisindeki sert bir
     üst sınırla zorlamak gerekiyor. Reflection ile
     `ChatClientAgentRunOptions(ChatOptions chatOptions)` bulundu,
     `ChatOptions.MaxOutputTokens = 200` ile `agent.RunAsync(message,
     session, runOptions)` çağrısına sert bir tavan kondu.
- **Sonuç**: `dotnet run` exit code 0 ile tamamlandı. Ajan gerçekten MCP
  sunucusuna bağlandı, bir tool çağırdı, **Postgres'teki gerçek veriyi**
  (Faz 3'te Inspector'da görülen aynı `payment-service` incident
  kayıtları — `0b5b184b-...`, `165f3922-...`) aldı ve bu veriye dayanan bir
  metin üretti. Çekirdek mimari hedef (ajan → MCP tool → gerçek DB verisi →
  ajan cevabı) kanıtlandı.

**Doğrulama sonucu**: `dotnet build` — 0 hata. `dotnet run` — exit code 0,
gerçek, DB kökenli veri içeren bir cevap üretti.

**Bilinen, Faz 5'e ertelenen 4 kalite sorunu** (kullanıcının kararıyla —
"faz 5'te bu 4 maddeye de bakalım"):
1. Ajana şu an MCP sunucusundaki **her iki** tool da veriliyor
   (`tools: [.. tools]`, hepsi); "Kod Risk Ajanı" beklenen
   `get_release_signals` yerine `get_service_incident_histories`'i
   çağırdı. Mimari plana göre (Faz 5-6'da) her uzman ajan sadece kendi
   tool'unu görmeli.
2. Cevabın başında anlamsız, ham bir tool-call JSON parçası sızmış
   (`}; {"name":"generate_risk_report",...}`) — model var olmayan bir
   tool hayal etmiş gibi görünüyor.
3. Cevap `MaxOutputTokens: 200` sınırında yarıda kesildi — beklenen bir
   trade-off, ama daha iyi bir denge (belki 400-500) denenebilir.
4. Metin Türkçe-İngilizce karışık ve biraz tutarsız — `llama3.2:3b`'nin
   küçük model sınırı, CPU üzerinde daha da belirginleşiyor.

**Review notları**:
- Bu faz, önceki fazlardan farklı olarak **iki ayrı gerçek altyapı
  hatası** (ikisi de timeout kaynaklı, ama kök sebepleri farklı: önce
  gerçekten yetersiz süre, sonra modelin talimata uymaması) üst üste
  çıktı — ikisi de Claude tarafından reflection ile doğrulanan gerçek
  API'lerle (HttpClient overload, ChatClientAgentRunOptions) çözüldü,
  tahmin yapılmadı.
- Kullanıcı, düşük kaliteli ama **çalışan** bir sonucu doğru şekilde
  "bugünün kanıtı yeterli, kalan ince ayarları yarına bırakalım" diye
  değerlendirdi — her şeyi aynı oturumda mükemmelleştirmeye çalışmak
  yerine çekirdek kanıtı kabul edip devam etmek, önceki fazlarda da
  görülen sağlıklı bir karar verme deseni.

---

## Roller

- **Kullanıcı**: Her fazın kodunu bizzat yazar. Takıldığında spesifik soru
  sorar — Claude tam çözümü direkt yazmaz, önce neyin yanlış olduğunu
  açıklar.
- **Claude**: Mimarı belirledi, her faz sonunda review yapar, config/
  boilerplate işlerinde (Directory.Build.props, DbContext/migration gibi
  üçüncü kez tekrarlanan mekanik desenler) ve Madde 0 risk doğrulamasında
  doğrudan yardımcı olur.
- **Git**: Her faz grubunun sonunda küçük, anlamlı bir commit.
