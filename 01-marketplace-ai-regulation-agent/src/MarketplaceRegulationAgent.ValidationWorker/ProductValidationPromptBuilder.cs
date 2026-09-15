namespace MarketplaceRegulationAgent.ValidationWorker;

public static class ProductValidationPromptBuilder
{
    public static string Build(string productName, string category, decimal price, List<string> similarRejectedProducts)
    {
        var similarText = similarRejectedProducts.Count > 0
            ? $"Daha önce reddedilen benzer ürünler: {string.Join(", ", similarRejectedProducts)}."
            : "Daha önce reddedilen benzer bir ürün bulunamadı.";

        return $"""
            Sen bir e-ticaret pazar yeri için ürün denetleme asistanısın.
            Aşağıdaki ürünü incele ve şu kriterlere göre değerlendir:
            1. Yasaklı/yasal olmayan içerik var mı (silah, uyuşturucu, sahte marka ürünü vb.)?
            2. Kategori, ürün açıklamasıyla uyumlu mu?
            3. Fiyat, ürün için makul mü (aşırı düşük ya da yüksek mi)?

            Ürün adı: {productName}
            Kategori: {category}
            Fiyat: {price} TL
            {similarText}

            Bulgularını yapılandırılmış şekilde bildir.
            """;
    }
}
