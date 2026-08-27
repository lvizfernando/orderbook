using MatchingEngine.Core.Abstractions;
using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Gateway;

public class ExchangeGateway
{
    private readonly IMatchingEngine _engine;

    public ExchangeGateway(IMatchingEngine engine)
    {
        _engine = engine;
    }

    public async Task<List<Trade>> ReceiveOrderAsync(Order order)
    {
        // TODO: chamada thread-safe para _engine.ProcessOrder(order)
        return _engine.ProcessOrder(order);
    }
}