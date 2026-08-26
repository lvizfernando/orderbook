using MatchingEngine.Core.Abstractions;
using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Engine;

public class MatchingEngineImpl : IMatchingEngine
{
    // TODO: escolher estrutura de dados do book (Buy side / Sell side)
    // pensando em Big-O de inserção e busca do melhor preço.

    public List<Trade> ProcessOrder(Order order)
    {
        throw new NotImplementedException();
    }

    // Usado no Chaos Test para validar que lotes comprados == lotes vendidos
    public bool ValidarIntegridadeDoBook()
    {
        throw new NotImplementedException();
    }

    public List<Order> GetBuyOrders()
    {
        throw new NotImplementedException();
    }

    public List<Order> GetSellOrders()
    {
        throw new NotImplementedException();
    }
}