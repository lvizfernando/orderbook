using MatchingEngine.Core.Domain;
using MatchingEngine.Core.Engine;
using MatchingEngine.Core.Gateway;

namespace MatchingEngine.Tests;

public class ChaosTest
{
    [Fact]
    public void DeveProcessarOrdensEmParaleloSemCorromperSaldo() 
    {
        var engine = new MatchingEngineImpl(); // Sua implementação
        var gateway = new ExchangeGateway(engine);
        
        // Simula 10.000 ordens sendo enviadas no mesmo milissegundo
        var orders = Gerar10MilOrdensAleatorias(); 

        Parallel.ForEach(orders, order => 
        {
            gateway.ReceiveOrderAsync(order).Wait();
        });

        // O total de lotes comprados TEM QUE SER IGUAL ao total de lotes vendidos.
        Assert.True(engine.ValidarIntegridadeDoBook()); 
    }

    private List<Order> Gerar10MilOrdensAleatorias()
    {
        var random = new Random();
        var orders = new List<Order>();

        for (int i = 0; i < 10000; i++)
        {
            orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                Side = (Side)random.Next(0, 2), // 0 para Buy, 1 para Sell
                Price = (decimal)(random.NextDouble() * 100),
                Quantity = random.Next(1, 100),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        return orders;
    }
}
