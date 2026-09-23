# LinkedIn Paylaşım Taslağı — Proje 1

> Bu bir taslak — kendi üslubuna göre kısalt/uzat, [GitHub linki] kısmını repo
> public olduğunda doldur. Görselleri (Aspire dashboard, Scalar UI ekran
> görüntüleri) gönderiye eklemek yazının etkisini artırır.

---

.NET tarafında "AI Applied Engineer" olma yolunda attığım ilk adımı
paylaşmak istiyorum: **Multi-Tenant Marketplace AI Regulation Agent.**

Bir pazar yerinde (Trendyol/Hepsiburada tarzı) satıcıların yüklediği
ürünleri, yerel bir LLM'e (Ollama üzerinde çalışan llama3.2) sorup
asenkron olarak denetleyen bir servis kurdum. Sistem her ürün için:

🔍 pgvector ile geçmişte reddedilmiş en benzer ürünleri buluyor
🤖 LLM'e typed/structured output isteği atıyor (rastgele metin değil,
şemaya bağlı JSON dönüyor)
✅ Sonuca göre ürünü otomatik onaylıyor/reddediyor, gerekçesiyle birlikte

**Kullandığım teknolojiler:** .NET 10, .NET Aspire (orkestrasyon),
MassTransit + RabbitMQ (asenkron mesajlaşma), PostgreSQL + pgvector
(embedding benzerlik araması), Microsoft.Extensions.AI + Ollama (typed
structured output), multi-tenant izolasyon (EF Core global query filter).

Bu projede bana en çok şey öğreten kısım, beklediğim gibi "AI'a prompt
yazmak" değil, **etrafındaki mühendislik** oldu: yerel bir modelin CPU'da
~90-120 saniye sürebilen yanıt süresine göre timeout/resilience
stratejisi kurmak, asenkron bir pipeline'da "henüz işlenmedi" ile "hata"
durumunu doğru ayırt etmek, tenant izolasyonunu DI zamanlaması yüzünden
bozan sinsi bir bug'ı bulup düzeltmek gibi.

Bilinçli bir kararla bu projeyi baştan sona ben yazdım — mimariyi ve
"gün gün" öğrenme planını Claude ile birlikte kurduk, ama kodun büyük
kısmını kendim yazdım, takıldığım yerlerde ipucu alıp kendim çözdüm.
Amaç sadece çalışan bir sistem değil, gerçekten anlayarak ilerlemekti.

Proje kaynak kodu ve gün gün ilerleme günlüğü: [GitHub linki]

#dotnet #AI #SoftwareEngineering #MachineLearning #Backend
