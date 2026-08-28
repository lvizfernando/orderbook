using MatchingEngine.Core.Domain;
using MatchingEngine.Core.Engine;
using MatchingEngine.Core.Gateway;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

namespace MatchingEngine.Tests;

public class RoteiroDeTestes
{
    [Fact]
    public async Task AcomodacaoNoBook()
    {
        Order buyOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 20.0m,
            Quantity = 10,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        // Processa a ordem de compra
        var trades = await gateway.ReceiveOrderAsync(buyOrder);

        Assert.Empty(trades); // Nenhuma ordem de venda para casar, então não deve haver trades
    }

    [Fact]
    public async Task MatchPerfeito()
    {
        Order buyOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 20.0m,
            Quantity = 100,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Order sellOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.0m,
            Quantity = 100,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        // Processa a ordem de compra
        var tradesFromBuyOrder = await gateway.ReceiveOrderAsync(buyOrder);
        Assert.Empty(tradesFromBuyOrder); // Nenhuma ordem de venda para casar, então não deve haver trades

        // Processa a ordem de venda
        var tradesFromSellOrder = await gateway.ReceiveOrderAsync(sellOrder);
        
        // Agora deve haver um trade
        Assert.Single(tradesFromSellOrder);
        Assert.Equal(100, tradesFromSellOrder[0].Quantity);

        Assert.Empty(engine.GetBuyOrders()); // O book de compra deve estar vazio
        Assert.Empty(engine.GetSellOrders()); // O book de venda deve estar vazio
    }

    [Fact]
    public async Task ExecucaoParcial()
    {
        Order sellOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.0m,
            Quantity = 100,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Order buyOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 20.0m,
            Quantity = 150,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        // Processa a ordem de venda
        await gateway.ReceiveOrderAsync(sellOrder);
        // Processa a ordem de compra
        var tradesFromBuyOrder = await gateway.ReceiveOrderAsync(buyOrder);

        // Agora deve haver um trade
        Assert.Single(tradesFromBuyOrder);
        Assert.Equal(100, tradesFromBuyOrder[0].Quantity);

        // O book de compra deve ter 50 unidades restantes
        var remainingBuyOrders = engine.GetBuyOrders();
        Assert.Single(remainingBuyOrders);
        Assert.Equal(50, remainingBuyOrders[0].Quantity);
    }

    [Fact]
    public async Task PrioridadeDePreco()
    {
        Order sellOrderA = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.5m,
            Quantity = 1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Order sellOrderB = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.0m, // Preço melhor (mais baixo)
            Quantity = 1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        Order buyOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 21.0m,
            Quantity = 1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        // Processa as ordens de venda
        await gateway.ReceiveOrderAsync(sellOrderA);
        await gateway.ReceiveOrderAsync(sellOrderB);
        
        // Processa a ordem de compra
        var tradesFromBuyOrder = await gateway.ReceiveOrderAsync(buyOrder);

        Assert.Single(tradesFromBuyOrder);
        Assert.Equal(20.0m, tradesFromBuyOrder[0].Price); // Garante que a ordem de venda com o preço mais baixo foi casada primeiro
    }

    [Fact]
    public async Task PrioridadeDeTempoFIFO()
    {
        Order sellOrderA = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.0m,
            Quantity = 1,
            Timestamp = 1000
        };

        Order sellOrderB = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 20.0m,
            Quantity = 1,
            Timestamp = 10001
        };

        Order buyOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 21.0m,
            Quantity = 1,
            Timestamp = 10002
        };

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        // Processa as ordens de venda
        await gateway.ReceiveOrderAsync(sellOrderA);
        await gateway.ReceiveOrderAsync(sellOrderB);
        
        // Processa a ordem de compra
        var tradesFromBuyOrder = await gateway.ReceiveOrderAsync(buyOrder);

        Assert.Single(tradesFromBuyOrder);
        Assert.Equal(sellOrderA.Id, tradesFromBuyOrder[0].MakerOrderId); // Garante que a ordem de venda mais antiga foi casada primeiro
    }

    [Fact]
    public async Task PrioridadeDeTempoFIFOCompra()
    {
        Order buyOrderA = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 20.0m,
            Quantity = 1,
            Timestamp = 1000
        };

        Order buyOrderB = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Buy,
            Price = 20.0m,
            Quantity = 1,
            Timestamp = 10001
        };

        Order sellOrder = new Order
        {
            Id = Guid.NewGuid(),
            Side = Side.Sell,
            Price = 19.0m,
            Quantity = 1,
            Timestamp = 10002
        }; 

        MatchingEngineImpl engine = new MatchingEngineImpl();
        ExchangeGateway gateway = new ExchangeGateway(engine);

        var tradesFromBuyOrder = await gateway.ReceiveOrderAsync(buyOrderA);
        var tradesFromBuyOrderB = await gateway.ReceiveOrderAsync(buyOrderB);
        var result = await gateway.ReceiveOrderAsync(sellOrder);

        Assert.Empty(tradesFromBuyOrder);
        Assert.Single(result);
        Assert.Equal(buyOrderA.Id, result[0].MakerOrderId);

    }
}