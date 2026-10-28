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
| 2 | Infrastructure (DbContext, migration) + sentetik veri üretici | 🔶 Kısmi (DbContext/migration bitti, üretici mantığı kaldı) |
| 3 | MCP sunucusu: `get_release_signals`, `get_service_incident_history` araçları | ⬜ |
| 4 | Agent Framework'e giriş: tek ajan (Kod Risk Ajanı) + MCP aracı çağırma | ⬜ |
| 5 | Kalan iki uzman ajan (Test Risk, Geçmiş Risk) | ⬜ |
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

## Faz 2 (kısmi) — DbContext + migration

- **Yap**: `ReleaseRiskAnalyst.Infrastructure` projesi, `ReleaseRiskDbContext`,
  design-time factory, ilk migration, ayrı bir Postgres container'ı.
- **Durum**: 🔶 Kısmi — bu kısım Claude tarafından doğrudan yapıldı
  (mekanik, üçüncü kez tekrarlanan desen, düşük öğrenme değeri — Proje
  1/2'deki aynı istisna). **Sentetik veri üretici mantığı bir sonraki
  oturuma bırakıldı** — bu, asıl düşünce gerektiren kısım, acele
  edilmeyecek.

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
