# Desafio Técnico: Matching Engine (Order Book)

## Contexto e Objetivos
No mercado financeiro, a latência e a precisão são inegociáveis. Milissegundos perdidos por escolhas ruins de algoritmos ou problemas de concorrência podem resultar em perdas financeiras milionárias. 

O objetivo deste laboratório é construir o núcleo de um **Matching Engine** (Motor de Cruzamento de Ordens), focando na aplicação prática de **Estruturas de Dados** (Big-O) e no manuseio de **Concorrência** e **Alta Performance**.

⚠️ **PROIBIDO O USO DE IA (ChatGPT, Copilot, Gemini, etc.)**
> Este é um exercício de raciocínio lógico e engenharia de base. O uso de IA para gerar o código anula o propósito do treinamento. Consultas a documentações oficiais e livros são permitidas.

---

## Escopo e Regras de Negócio (V1)

Para manter o foco na lógica central, o escopo inicial tem restrições estritas:

| Requisito | Definição |
| :--- | :--- |
| **Ativo** | Fixo. O motor processará ordens de um único ativo (ex: `PETR4`). |
| **Tipos de Ordem** | Apenas *Limit Orders* (Ordens com preço limite estipulado). |
| **Operações** | Apenas Inserção (Compra ou Venda). Cancelamento e alteração estão fora do escopo. |
| **Regra de Matching** | **Price-Time Priority**. O melhor preço sempre tem prioridade. Em caso de empate no preço, a ordem que chegou primeiro é executada. |
| **Persistência** | O processamento deve ocorrer 100% em memória. Não haverá banco de dados. |

---

## Modelos de Domínio (Ponto de Partida)

Vocês devem utilizar os modelos abaixo como base para a implementação. Observem o uso de identificadores universais (`UUID/Guid`).

```csharp
public enum Side { Buy, Sell }

public class Order 
{
    public Guid Id { get; set; }
    public Side Side { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public long Timestamp { get; set; } 
}

public class Trade 
{
    public Guid Id { get; set; }
    public Guid MakerOrderId { get; set; }
    public Guid TakerOrderId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public interface IMatchingEngine 
{
    // Processa a ordem e retorna os trades gerados (se houver cruzamento)
    List<Trade> ProcessOrder(Order order); 
}

```

---

## Concorrência (A Porta de Entrada)

Seu motor não rodará em um ambiente isolado. Ele receberá um bombardeio de ordens simultâneas. Para simular isso, sua implementação deve ser acoplada à camada de `ExchangeGateway`:

```csharp
public class ExchangeGateway 
{
    private readonly IMatchingEngine _engine;

    public ExchangeGateway(IMatchingEngine engine) 
    {
        _engine = engine;
    }

    // ATENÇÃO: Este método será chamado por múltiplas threads simultaneamente.
    // Como vocês vão garantir que o motor cruze as ordens sem corromper o estado em memória?
    public async Task<List<Trade>> ReceiveOrderAsync(Order order) 
    {
        // TODO: Implementar a chamada segura para _engine.ProcessOrder(order)
        throw new NotImplementedException();
    }
}

```

### Teste de Estresse (Chaos Test)

Sua solução final **deve** passar no teste de estresse abaixo sem corromper as quantidades de lotes negociados e sem gerar *Deadlocks*:

```csharp
[Fact]
public void DeveProcessarOrdensEmParaleloSemCorromperSaldo() 
{
    var engine = new MatchingEngine(); // Sua implementação
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

```

---

## Roteiro de Testes Unitários (TDD)

Sua equipe deve implementar a lógica gradativamente, garantindo que os testes abaixo passem um a um, nesta exata ordem:

### Teste 1: Acomodação no Book

* **Ação:** Enviar `Ordem de Compra` (Bid) de 100 lotes a R$ 20,00.
* **Resultado Esperado:** Retorna lista de *Trades* vazia. A ordem repousa no *book*.

### Teste 2: O Match Perfeito (Execução Total)

* **Estado:** *Book* possui uma `Venda` de 100 lotes a R$ 20,00.
* **Ação:** Enviar `Ordem de Compra` de 100 lotes a R$ 20,00.
* **Resultado Esperado:** 1 *Trade* gerado de 100 lotes a R$ 20,00. O *book* deve ficar completamente vazio.

### Teste 3: Execução Parcial (Sobrando saldo no Taker)

* **Estado:** *Book* possui uma `Venda` de 100 lotes a R$ 20,00.
* **Ação:** Enviar `Ordem de Compra` de **150** lotes a R$ 20,00.
* **Resultado Esperado:** 1 *Trade* de 100 lotes é gerado. Uma nova `Ordem de Compra` com os **50** lotes restantes deve repousar no *book* a R$ 20,00.

### Teste 4: Prioridade de Preço (O melhor preço vence)

* **Estado:** *Book* possui duas ordens de Venda: Ordem A (R$ 20,50) e Ordem B (**R$ 20,00**).
* **Ação:** Enviar `Ordem de Compra` a R$ 21,00.
* **Resultado Esperado:** O *trade* ocorre obrigatoriamente cruzando com a **Ordem B** (R$ 20,00), garantindo o melhor preço para quem comprou.

### Teste 5: Prioridade de Tempo (FIFO)

* **Estado:** *Book* possui duas ordens de Venda no mesmo preço: Ordem A (R$ 20,00, Timestamp: 1000) e Ordem B (R$ 20,00, Timestamp: 1001).
* **Ação:** Enviar `Ordem de Compra` parcial que consuma apenas uma delas.
* **Resultado Esperado:** O *trade* deve consumir primeiro a **Ordem A** (menor Timestamp).

---

## Critérios de Avaliação

Ao final da dinâmica, as soluções serão avaliadas sob três pilares:

1. **Complexidade Computacional (Big-O):** Suas buscas por preços e inserções dependem de varrer arrays inteiros `O(n)` ou utilizam estruturas otimizadas `O(log n)` / `O(1)`?
2. **Gerenciamento de Estado:** Vocês mutaram (alteraram) a ordem original ou geraram novas ordens para lidar com execuções parciais de forma segura?
3. **Gestão de Concorrência:** O sistema sobreviveu ao *Chaos Test*? A abordagem de *locking* (se utilizada) estrangulou a performance do motor?

Boa sorte e bom código!