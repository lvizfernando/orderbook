# orderbook

### Questão 1 — Complexidade Computacional (Big-O)

> *Suas buscas por preços e inserções dependem de varrer arrays inteiros `O(n)` ou utilizam estruturas otimizadas `O(log n)` / `O(1)`?*

Começamos com a ideia de um dicionário no gateway tendo o preço como chave. Mas um `Dictionary` comum se comporta como uma tabela hash e não tem ordem definida — para achar o melhor preço seria preciso varrer todas as chaves, o que acaba sendo um `O(n)`, justamente na operação mais usada dentro do motor.
Pesquisando, chegamos ao **`SortedDictionary`, que é uma árvore binária de busca balanceada**. Cada inserção vira um nó na árvore, e ela **se auto-balanceia** a partir de uma regra pré determinada. O que definimos é que para qualquer nó tudo à direita é maior e tudo à esquerda é menor, respeitando um comparador, ela sempre devolve em ordem.

Dessa forma **pegamos sempre o primeiro elemento e já sabemos o melhor preço**. A gente não busca — a estrutura já cuida disso no momento da inserção.

São dois books, um de compra e um de venda. No book de compra colocamos um `IComparer` que **inverte a comparação**, então a árvore passa a tratar o **maior preço como "o primeiro"**. É exatamente o que queremos ali, porque quem oferece mais tem prioridade. Com isso os dois lados são lidos da mesma forma, sem caso especial.

O preço é a chave da desse dicionário porque ele é a prioridade na hora de verificar quem é o escolhido. Como cada preço pode ter N ordens, e entre elas a prioridade é a hora de chegada, o valor de cada nó é uma **fila**. A árvore resolve a questão relacionada ao melhor preço já a fila resolve quem fez o primeiro pedido.

Assim, inserir uma ordem, buscar um nível de preço e remover um nível são `O(log n)` — a árvore desce comparando preço até achar a posição certa, e como ela se rebalanceia sozinha, a altura nunca cresce descontroladamente. Em uma **lista ordenada**, cada inserção no meio exigiria deslocar todos os elementos seguintes, o que acaba sendo `O(n)`.


### Questão 2 — Gerenciamento de Estado

> *Vocês mutaram (alteraram) a ordem original ou geraram novas ordens para lidar com execuções parciais de forma segura?*

**Mutamos a ordem original.**

Quando uma ordem bate no book e executa só uma parte, não criamos uma ordem nova para representar o saldo restante. Mutamos a `Quantity` direto no objeto que já estava lá por exemplo:

MatchingEngineImpl.cs - Linha 62
```csharp
sellOrder.Quantity -= order.Quantity;
```

**Por quê:**

- Não aloca um objeto novo a cada execução parcial. Num motor que processa dezenas de milhares de ordens, cada alocação evitada é menos pressão no GC.
- Mantém o `Id` original, o que é importante para rastreabilidade. A ordem continua sendo a mesma ordem do começo ao fim — inclusive porque os `Trade` já emitidos apontam para ela via `MakerOrderId`.
- Mantém a posição na fila, recriar a ordem a mandaria para o fim da fila daquele preço, e uma execução parcial acabaria rebaixando quem chegou primeiro.

Mutar um objeto compartilhado só é seguro se houver certeza de que ninguém mais está mexendo nele ao mesmo tempo — senão cada `-=` desses seria uma condição de corrida clássica: ler, subtrair, escrever, com outra thread no meio.

É exatamente isso que a arquitetura do gateway garante. Como só uma thread por vez toca o book, essa mutação nunca vira uma condição de corrida.


### Questão 3 — Gestão de Concorrência

> *O sistema sobreviveu ao Chaos Test? A abordagem de locking (se utilizada) estrangulou a performance do motor?*

Na versão 1 do nosso código deixamos apenas o `lock` cuidar da concorrência lá no `ProcessOrder`. O código até funcionava, o resultado era correto, mas estava muito lento, toda thread estava disputando pelo mesmo lock, e esse lock cobria a operação inteira de matching. O trabalho já acontecia em série, mas como tínhamos somente o lock cuidando de tudo acabava prejudicando a performance.

Pesquisando, achamos a `BlockingCollection`, que tem uma indicação justamente para quando temos N produtores e um ou mais consumidores. É responsabilidade dessa estrutura manter tudo sincronizado, sem precisarmos escrever o lock. Usamos ela no gateway e ficamos com N threads produtoras e apenas uma cuidando do book — uma única thread consumindo a fila e chamando o `ProcessOrder`. Assim matamos a disputa por lock.

Ao utilizarmos o `TaskCompletionSource` em conjunto com a `BlockingCollection`, a função de adicionar no produtor e o processamento ficam desacoplados no tempo, mas ainda assim quem chamou consegue saber quando a sua ordem terminou e qual foi o resultado. Isso é o que permite não quebrar a paralelização do teste já que continuamos esperando tudo terminar antes de validar a integridade.

O resultado medido através dos testes:

```
Resumo do teste: total: 7; falhou: 0; bem-sucedido: 7; ignorado: 0; duração: 0,9s

```

Sem deadlock e sem corrompimentos a validação de integridade fecha com o que precismamos `o que sobrou no book == o que entrou − o que foi executado` nos dois lados.
