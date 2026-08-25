using System;

namespace MatchingEngine.Core.Domain;

public class Order
{
    public Guid Id { get; set; }
    public Side Side { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public long Timestamp { get; set; }
}