namespace Microservices.Shared.Domain.Persistence;

// I dont know what I will use EEF(havier) | Linq2DB(supports bulks),
// so I'll use Fluent API later. no attrs now  
public record TradeDto
{
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; }
    
    public Guid TradeId { get; set; }
    
    public double Price { get; set; }
    public double Volume { get; set; }

    public bool TakerIsBuyer { get; set; }
}
