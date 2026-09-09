namespace MarketplaceRegulationAgent.Domain;

    public class Tenant
    {
        public Guid Id { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
}