namespace Microservices.Shared.Domain.MarketData;

public record struct PriceLevel(double Price, double Volume)
{
    public double Price = Price;
    public double Volume = Volume;
    
    public static PriceLevel Empty => new();
    
    public PriceLevel() : this(0, 0) { }
}
