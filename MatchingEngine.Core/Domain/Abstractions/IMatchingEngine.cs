using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Abstractions;

public interface IMatchingEngine
{
    List<Trade> ProcessOrder(Order order);
    List<Order> GetBuyOrders();
    List<Order> GetSellOrders();
}