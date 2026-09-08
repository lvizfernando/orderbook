using System.Collections.Concurrent;
using MatchingEngine.Core.Abstractions;
using MatchingEngine.Core.Domain;

namespace MatchingEngine.Core.Gateway;

public class ExchangeGateway
{
    private readonly IMatchingEngine _engine;
    private readonly BlockingCollection<(Order Order, TaskCompletionSource<List<Trade>> Completion)> _queue = new();

    public ExchangeGateway(IMatchingEngine engine)
    {
        _engine = engine;
         var worker = new Thread(ConsumeLoop)
        {
            IsBackground = true,
            Name = "MatchingEngine-Worker"
        };
        worker.Start();

    }

    public Task<List<Trade>> ReceiveOrderAsync(Order order)
    {
        var tcs = new TaskCompletionSource<List<Trade>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add((order, tcs));
        return tcs.Task;

    }

     private void ConsumeLoop()
    {
        foreach (var (order, tcs) in _queue.GetConsumingEnumerable())
        {
            try
            {
                var trades = _engine.ProcessOrder(order);
                tcs.SetResult(trades);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }

}