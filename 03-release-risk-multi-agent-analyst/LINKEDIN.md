# LinkedIn Paylaşım Taslağı — Proje 3

> Bu bir taslak — kendi üslubuna göre kısalt/uzat, [GitHub linki] kısmını
> doldur. Ekran görüntüsü (örn. MCP Inspector'daki tool listesi ya da API
> yanıtının JSON çıktısı) eklemek yazının etkisini artırır.

---

AI Applied Engineer yolculuğumda üçüncü projeyi bitirdim: **Release-Risk
Çok-Ajanlı Analist.**

Bu projede ilk kez **Microsoft Agent Framework** ve **MCP (Model Context
Protocol)** kullandım — bir yazılım release'inin risk seviyesini, üç farklı
uzman ajanın bakış açısını birleştirerek değerlendiren bir sistem kurdum:

🧑‍💻 **Kod Risk Ajanı** — değişen satır sayısı, reviewer sayısı, test
coverage, başarısız CI sayısına bakıyor
🧪 **Test Risk Ajanı** — test coverage ve CI sinyallerine odaklanıyor
📉 **Geçmiş Risk Ajanı** — servisin geçmiş incident kayıtlarına bakıyor
🤝 **Orkestratör** — üçünün bulgusunu birleştirip, **hangi ajanın bulgusuna
dayandığını açıkça belirterek** tek bir nihai karar veriyor

Hepsi, veritabanına doğrudan erişmiyor — bir **MCP sunucusu** üzerinden,
tanımlı araçları (`get_release_signals`, `get_service_incident_histories`)
çağırarak veriye ulaşıyor.

En öğretici an: orkestratörden nihai risk seviyesini JSON olarak istediğimde,
model sessizce **bu alanı tamamen atlıyordu** — ve C#'ın non-nullable enum
davranışı bunu farkında olmadan "Low" diye varsayılan bir değere
düşürüyordu. Yani gördüğüm karar, gerçek bir karar değil, **sessiz bir
hataydı.** Çözüm, bu projenin ana dersi oldu: **kararın kendisini modelden
değil koddan iste.** Üç ajanın risk seviyesini C# kodunda karşılaştırıp
nihai kararı deterministik olarak hesapladım; LLM'e sadece zaten verilmiş
kararı gerekçelendiren bir paragraf yazdırdım. Format için zaten aynı dersi
(Faz 5'te) öğrenmiştim — bu kez kararın kendisine uyguladım.

**Kullandığım teknolojiler**: .NET 10, Microsoft.Agents.AI (Agent
Framework), ModelContextProtocol (MCP C# SDK), Microsoft.Extensions.AI +
Ollama (`llama3.2:3b`), EF Core + PostgreSQL, Minimal API.

Önceki iki projede olduğu gibi burada da kodun büyük kısmını kendim
yazdım — Claude mimar/mentor/code reviewer rolünde eşlik etti, altyapı/
config gibi düşük öğrenme değerli kısımlarda (Docker container kurulumu,
proje iskeletleri, bazı API doğrulamaları) doğrudan yardımcı oldu, bunu
süreç günlüğünde şeffafça belirttim.

Kaynak kod + tam süreç günlüğü (her hatanın, her kararın kaydı): [GitHub linki]

#dotnet #AI #AgenticAI #MCP #SoftwareEngineering
