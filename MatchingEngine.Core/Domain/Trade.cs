namespace MatchingEngine.Core.Domain;

public class Trade
{
    public Guid Id { get; set; }
    public Guid MakerOrderId { get; set; }
    public Guid TakerOrderId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}