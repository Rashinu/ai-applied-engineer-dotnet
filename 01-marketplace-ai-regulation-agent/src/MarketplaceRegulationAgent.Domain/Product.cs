namespace MarketplaceRegulationAgent.Domain;

    public class Product
    {
       public Guid Id {get; set;}
       public string Name {get; set;} = string.Empty;
       public decimal Price {get; set;}

      public Guid  TenantId {get; set;}

      public string Category {get; set;} = string.Empty;

    public ProductStatus Status {get; set;}

    public DateTime CreatedAt {get; set;}
    public int Quantity {get; set;}
    }
