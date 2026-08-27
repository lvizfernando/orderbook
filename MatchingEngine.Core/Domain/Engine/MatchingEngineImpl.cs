using MatchingEngine.Core.Abstractions;
using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Engine;

public class MatchingEngineImpl : IMatchingEngine
{
    // TODO: escolher estrutura de dados do book (Buy side / Sell side)
    // pensando em Big-O de inserção e busca do melhor preço.

    private SortedDictionary<Tuple<decimal, long>, List<Order>> buyOrders = new SortedDictionary<Tuple<decimal, long>, List<Order>>();
    private SortedDictionary<Tuple<decimal, long>, List<Order>> sellOrders = new SortedDictionary<Tuple<decimal, long>, List<Order>>();
    private int quantidadeTrade = 0;

    public List<Trade> ProcessOrder(Order order)
    {
        if(order.Side == Side.Buy)
        {
            var matchingSellOrders = sellOrders.FirstOrDefault().Value;

            if(matchingSellOrders == null || matchingSellOrders.Count == 0 || matchingSellOrders[0].Price > order.Price)
            {
                buyOrders.Add(new Tuple<decimal, long>(order.Price, order.Timestamp), new List<Order> { order });
                return new List<Trade>();
            }
            else
            {
                var sellOrder = matchingSellOrders[0];
                
                if(order.Quantity > sellOrder.Quantity)
                {
                    order.Quantity -= sellOrder.Quantity;
                    matchingSellOrders.RemoveAt(0);
                    buyOrders.Add(new Tuple<decimal, long>(order.Price, order.Timestamp), new List<Order> { order });
                    quantidadeTrade = sellOrder.Quantity;
                }
                else if(order.Quantity < sellOrder.Quantity)
                {
                    sellOrder.Quantity -= order.Quantity;
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }
                else
                {
                    sellOrders.Remove(new Tuple<decimal, long>(sellOrder.Price, sellOrder.Timestamp));
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }

                var trade = new Trade
                {
                    Id = new Guid(),
                    MakerOrderId = sellOrder.Id,
                    TakerOrderId = order.Id,
                    Price = sellOrder.Price,
                    Quantity = quantidadeTrade,
                };

                quantidadeTrade = 0;
                return new List<Trade> { trade };
            } 
        }
        else
        {
            var matchingBuyOrders = buyOrders.LastOrDefault().Value;

            if(matchingBuyOrders == null || matchingBuyOrders.Count == 0 || matchingBuyOrders[0].Price < order.Price)
            {
                sellOrders.Add(new Tuple<decimal, long>(order.Price, order.Timestamp), new List<Order> { order });
                return new List<Trade>();
            }
            else
            {
                var buyOrder = matchingBuyOrders[0];
                
                if(order.Quantity > buyOrder.Quantity)
                {
                    order.Quantity -= buyOrder.Quantity;
                    matchingBuyOrders.RemoveAt(0);

                    sellOrders.Add(new Tuple<decimal, long>(order.Price, order.Timestamp), new List<Order> { order });

                    quantidadeTrade = buyOrder.Quantity;
                }
                else if(order.Quantity < buyOrder.Quantity)
                {
                    buyOrder.Quantity -= order.Quantity;
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }
                else
                {
                    buyOrders.Remove(new Tuple<decimal, long>(buyOrder.Price, buyOrder.Timestamp));
                    matchingBuyOrders.RemoveAt(0);
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }

                var trade = new Trade
                {
                    Id = new Guid(),
                    MakerOrderId = buyOrder.Id,
                    TakerOrderId = order.Id,
                    Price = buyOrder.Price,
                    Quantity = quantidadeTrade,
                };

                quantidadeTrade = 0;
                return new List<Trade> { trade };
            }
        }
    }

    // Usado no Chaos Test para validar que lotes comprados == lotes vendidos
    public bool ValidarIntegridadeDoBook()
    {
        throw new NotImplementedException();
    }

    public List<Order> GetBuyOrders()
    {
        return buyOrders.Values.SelectMany(o => o).ToList();
    }

    public List<Order> GetSellOrders()
    {
        return sellOrders.Values.SelectMany(o => o).ToList();
    }
}