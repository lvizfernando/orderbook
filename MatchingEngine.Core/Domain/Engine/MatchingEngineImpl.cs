using MatchingEngine.Core.Abstractions;
using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Engine;

public class MatchingEngineImpl : IMatchingEngine
{
    private static IComparer<decimal> buyPriceComparer = Comparer<decimal>.Create((a,b) => b.CompareTo(a));
    private SortedDictionary<decimal, Queue<Order>> buyOrders = new SortedDictionary<decimal, Queue<Order>>(buyPriceComparer);
    private SortedDictionary<decimal, Queue<Order>> sellOrders = new SortedDictionary<decimal, Queue<Order>>();

    private void AddOrderToBook(SortedDictionary<decimal, Queue<Order>> book, Order order)
    {
        if (book.TryGetValue(order.Price, out Queue<Order>? queueExist))
        {
            queueExist.Enqueue(order);
        }
        else
        {
            var queueNew = new Queue<Order>();
            queueNew.Enqueue(order);
            book.Add(order.Price, queueNew);
        }
    }
    public List<Trade> ProcessOrder(Order order)
    {
        int quantidadeTrade;

        if(order.Side == Side.Buy)
        {
            var matchingSellOrders = sellOrders.FirstOrDefault().Value;

            if(matchingSellOrders == null || matchingSellOrders.Count == 0 || matchingSellOrders.Peek().Price > order.Price)
            {
                AddOrderToBook(buyOrders, order);
                return new List<Trade>();
            }
            else
            {
                var sellOrder = matchingSellOrders.Peek();
                
                if(order.Quantity > sellOrder.Quantity)
                {
                    order.Quantity -= sellOrder.Quantity;
                    matchingSellOrders.Dequeue();
                    if(matchingSellOrders.Count == 0)
                    {
                        sellOrders.Remove(sellOrder.Price);
                    }
                    AddOrderToBook(buyOrders, order);
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
                    matchingSellOrders.Dequeue();
                    if(matchingSellOrders.Count == 0)
                    {
                        sellOrders.Remove(sellOrder.Price);
                    }
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }

                var trade = new Trade
                {
                    Id = Guid.NewGuid(),
                    MakerOrderId = sellOrder.Id,
                    TakerOrderId = order.Id,
                    Price = sellOrder.Price,
                    Quantity = quantidadeTrade,
                };

                return new List<Trade> { trade };
            } 
        }
        else
        {
            var matchingBuyOrders = buyOrders.FirstOrDefault().Value;

            if(matchingBuyOrders == null || matchingBuyOrders.Count == 0 || matchingBuyOrders.Peek().Price < order.Price)
            {
                AddOrderToBook(sellOrders, order);
                return new List<Trade>();
            }
            else
            {
                var buyOrder = matchingBuyOrders.Peek();
                
                if(order.Quantity > buyOrder.Quantity)
                {
                    order.Quantity -= buyOrder.Quantity;
                    matchingBuyOrders.Dequeue();
                    if(matchingBuyOrders.Count == 0)
                    {
                        buyOrders.Remove(buyOrder.Price);
                    }
                    AddOrderToBook(sellOrders, order);
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
                    matchingBuyOrders.Dequeue();
                    if(matchingBuyOrders.Count == 0)
                    {
                        buyOrders.Remove(buyOrder.Price);
                    }
                    quantidadeTrade = order.Quantity;
                    order.Quantity = 0;
                }

                var trade = new Trade
                {
                    Id = Guid.NewGuid(),
                    MakerOrderId = buyOrder.Id,
                    TakerOrderId = order.Id,
                    Price = buyOrder.Price,
                    Quantity = quantidadeTrade,
                };

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