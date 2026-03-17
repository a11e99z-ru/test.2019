namespace Microservices.Shared.Domain.Entities;

// I dont know what I will use EEF(havier) | Linq2DB(supports bulk),
// so I'll use Fluent API later. no attrs now  
public record Trade
{
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; }
    
    public Guid TradeId { get; set; }
    
    public double Price { get; set; }
    public double Volume { get; set; }

    public bool TakerIsBuyer { get; set; }
}
