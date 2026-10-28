# LinkedIn Paylaşım Taslağı — Proje 2

> Bu bir taslak — kendi üslubuna göre kısalt/uzat, [GitHub linki] kısmını
> doldur. Ekran görüntüsü (örn. `/insights` yanıtının Postman/tarayıcı
> çıktısı) eklemek yazının etkisini artırır.

---

AI Applied Engineer yolculuğumda ikinci projeyi bitirdim: **LLM FinOps
Copiloti.**

Şirketler agent tabanlı AI sistemlerine geçtikçe LLM API maliyetleri
kontrolden çıkıyor — retry fırtınaları, tekrar eden sorgular, kullanılmayan
cache fırsatları. Bu proje, bir şirketin LLM kullanım geçmişini analiz edip:

📈 **Maliyet tahmini** yapıyor (ML.NET SSA zaman serisi modeli, rolling-origin
backtest ile doğrulandı)
🚨 **Maliyet sıçramalarını** yakalıyor (istatistiksel anomali tespiti)
🔁 **Tekrar eden prompt'ları** kümeliyor (embedding + K-Means — cache
fırsatlarını ortaya çıkarıyor)
🤖 Bunların hepsini bir **LLM'e** sorup, **kanıta dayalı** tek bir öneri
üretiyor — ve en önemlisi: LLM kanıtsız bir sayı uydurursa, sistem bunu
**kendi kendine tespit edip reddediyor.**

Bu son kısmı canlı test ederken ilginç bir şey oldu: küçük, yerel bir model
(llama3.2:3b) prompt'umdaki "7 gün" ifadesindeki **7** rakamını, asıl
cevap olan **%0,91** trend değeriyle karıştırıp yanlış bir sayı üretti.
Sistem bunu kanıtla karşılaştırıp reddetti — tam da inşa ettiğim şeyin
neden gerekli olduğunu kanıtlayan bir an.

**Kullandığım teknolojiler**: .NET 10, ML.NET (zaman serisi tahmini +
anomali tespiti + kümeleme), Microsoft.Extensions.AI + Ollama (embedding +
typed/guarded LLM çıktısı), EF Core + PostgreSQL, Minimal API.

Proje 1'de olduğu gibi burada da kodun büyük kısmını kendim yazdım — Claude
mimar/mentor/code reviewer rolünde eşlik etti, altyapı/config gibi düşük
öğrenme değerli kısımlarda doğrudan yardımcı oldu (bunu süreç günlüğünde
şeffafça belirttim). Amaç yine aynı: sadece çalışan bir sistem değil,
gerçekten anlayarak ilerlemek.

Kaynak kod + tam süreç günlüğü (her kararın, her hatanın kaydı): [GitHub linki]

#dotnet #AI #MachineLearning #FinOps #SoftwareEngineering
