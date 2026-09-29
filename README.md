# AI Applied Engineer — .NET Journey

.NET Backend geliştiriciden **Applied AI Engineer**'a giden yolda, üzerinde
çalıştığım projelerin kronolojik kaydı. Her klasör bağımsız, uçtan uca
çalışan bir proje; sıra numarası geliştirilme sırasını gösterir.

## Bu projeler nasıl yazılıyor?

Bu projelerde Claude (Anthropic'in Claude Code'u) bana **mimar/mentor/code
reviewer** olarak eşlik ediyor — ama kodun gövdesini ben yazıyorum. Süreç
şöyle işliyor: mimariyi ve gün-gün/blok-blok öğrenme planını birlikte
kuruyoruz, sonra ben her adımı kendim yazıp Claude'a review ettiriyorum;
takıldığım yerde Claude çözümü direkt vermek yerine önce neyin yanlış
olduğunu açıklıyor, ben düzeltmeyi kendim deniyorum. İstisnalar (config/
boilerplate gibi düşük öğrenme değerli kısımlar, ya da zaman baskısı altında
Claude'un doğrudan yazdığı altyapı parçaları) her projenin `PROGRESS.md`
dosyasında **açıkça belirtiliyor** — gizlenmiyor.

Amaç sadece "çalışan bir sistem" biriktirmek değil; her projeyi gerçekten
anlayarak, mülakatta her satırını açıklayabilecek şekilde bitirmek.

## Projeler

| # | Proje | Açıklama | Durum |
|---|-------|----------|-------|
| 01 | [Marketplace AI Regulation Agent](./01-marketplace-ai-regulation-agent/) | Pazar yeri ürünlerini asenkron denetleyen AI ajanı (.NET Aspire, MassTransit, pgvector, Ollama) | ✅ Tamamlandı — Gün 1-7, uçtan uca çalışan gerçek AI kararı sistemi + sorgu endpoint'leri + Scalar API dokümantasyonu ([süreç günlüğü](./01-marketplace-ai-regulation-agent/PROGRESS.md)) |
| 02 | [LLM FinOps Copiloti](./02-llm-finops-copilot/) | LLM kullanım loglarından maliyet tahmini yapan, maliyet sıçramalarını yakalayan ve kanıta dayalı (grounded) öneriler üreten analiz servisi (ML.NET zaman serisi/anomali tespiti + embedding kümeleme + guarded LLM) | 🚧 Geliştiriliyor — Blok 1-3 ✅: sezonluk desen + anomali enjeksiyonlu sentetik veri (~440K satır) Postgres'e yazılıyor, ML.NET forecasting sırada ([süreç günlüğü](./02-llm-finops-copilot/PROGRESS.md)) |

## Neden bu repo var?

Tek tek proje reposu yerine burada hepsini bir arada, ilerleme sırasıyla tutuyorum
ki hem kendi gelişimimi takip edebileyim hem de bu alanla ilgilenenler (recruiter'lar
dahil) tek bakışta neyi, ne sırayla ve **nasıl** öğrendiğimi görebilsin. Her
projenin commit geçmişi ve `PROGRESS.md`'si gerçek zamanlı ilerlemeyi yansıtıyor
— hepsi bir anda yazılmış değil, haftalar içinde, düzenli oturumlarla inşa edildi.

## Proje klasör şablonu

Her yeni proje aşağıdaki minimum yapıyla açılır:

```
NN-proje-adi/
├── README.md      # ne yapıldığı, neden yapıldığı, nasıl çalıştırılır
├── PROGRESS.md     # blok/gün blok ilerleme günlüğü — kararlar, hatalar, çözümler
└── src/            # kaynak kod
```

## İletişim

[LinkedIn](https://www.linkedin.com/in/murat-keskin-dev/) — proje paylaşımlarımı
buradan takip edebilirsin.
