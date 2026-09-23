# AI Applied Engineer — .NET Journey

Bu repo, .NET tarafında AI Applied Engineer olma yolunda geliştirdiğim projelerin
kronolojik bir kaydı. Her klasör bağımsız bir proje; sıra numarası geliştirilme
sırasını gösterir.

## Projeler

| # | Proje | Açıklama | Durum |
|---|-------|----------|-------|
| 01 | [Marketplace AI Regulation Agent](./01-marketplace-ai-regulation-agent/) | Pazar yeri ürünlerini asenkron denetleyen AI ajanı (.NET Aspire, MassTransit, pgvector, Ollama) | ✅ Tamamlandı — Gün 1-7, uçtan uca çalışan gerçek AI kararı sistemi + sorgu endpoint'leri + Scalar API dokümantasyonu |
| 02 | [LLM FinOps Copiloti](./02-llm-finops-copilot/) | LLM kullanım loglarından maliyet tahmini yapan, maliyet sıçramalarını yakalayan ve kanıta dayalı (grounded) öneriler üreten analiz servisi (ML.NET zaman serisi/anomali tespiti + embedding kümeleme + guarded LLM) | 🚧 Geliştiriliyor — Blok 1 ✅ tamamlandı (proje iskeleti + `LlmCallLog` entity'si) |

## Neden bu repo var?

Tek tek proje reposu yerine burada hepsini bir arada, ilerleme sırasıyla tutuyorum
ki hem kendi gelişimimi takip edebileyim hem de bu alanla ilgilenenler (recruiter'lar
dahil) tek bakışta neyi, ne sırayla öğrendiğimi görebilsin.

## Proje klasör şablonu

Her yeni proje aşağıdaki minimum yapıyla açılır:

```
NN-proje-adi/
├── README.md      # ne yapıldığı, neden yapıldığı, nasıl çalıştırılır
└── src/           # kaynak kod
```
