# MiniEcommerce

> **Lista de Exercícios VI** — Garantia da Qualidade de Software / Gestão e Qualidade de Software
> Professor: **Daniel Henrique Matos de Paiva**

Mini e-commerce de linha de comando desenvolvido em **.NET 8 (C#)**, com **TDD** (testes unitários em **xUnit**) e histórico Git rigorosamente aderente ao padrão **Conventional Commits**.

**Status: ✅ concluído** — as 5 etapas do roteiro estão implementadas, com **169 testes unitários passando**.

---

## 👥 Equipe

| Papel | Nome | RA | GitHub |
| :--- | :--- | :--- | :--- |
| Desenvolvedor 1 | João Vitor Alves Rodrigues | 32513480 | [@Lebot007](https://github.com/Lebot007) |
| Desenvolvedor 2 | Rafael Luiz Ferreira de Souza | 32511503 | [@rafaellfsouza](https://github.com/rafaellfsouza) |

### Divisão do trabalho

| Desenvolvedor | Responsabilidades |
| :--- | :--- |
| **Dev 1** | Configuração inicial da solução, **Etapa 1** (Produto/Catálogo) e **Etapa 2** (Carrinho), além da primeira versão deste README |
| **Dev 2** | **Etapa 3** (Pedido/Checkout), **Etapa 4** (menu interativo de console) e **Etapa 5** (refatoração do núcleo + documentação final) |

Os commits de cada desenvolvedor são identificáveis pelo autor em `git log --pretty="%h %an %s"`.

---

## 📋 Requisitos funcionais

| # | Requisito | Situação |
| :-: | :--- | :--- |
| 1 | **Cadastro de Produtos** — nome, preço e quantidade em estoque, com validação que impede preço negativo ou estoque menor que zero | ✅ Implementado (`Produto`, `CatalogoProdutos`) |
| 2 | **Gerenciamento de Carrinho de Compras** — adicionar produtos informando a quantidade, validar estoque disponível e remover itens | ✅ Implementado (`CarrinhoDeCompras`, `ItemCarrinho`) |
| 3 | **Fechamento de Pedido (Checkout)** — processar a compra, dar baixa automática no estoque e calcular o valor total do pedido | ✅ Implementado (`Pedido`, `ItemPedido`, `ProcessadorDePedidos`) |
| 4 | **Catálogo e Consulta** — exibir a lista de produtos com preços e estoques atuais | ✅ Implementado (`CatalogoProdutos.ObterListagemFormatada` + opção 2 do menu) |

---

## 🏗 Arquitetura

Solução composta por dois projetos, conforme proposto na lista:

```text
MiniEcommerce/
├── MiniEcommerce.sln
├── MiniEcommerce.Console/            # Aplicação (domínio + entrada console)
│   ├── Models/
│   │   ├── Produto.cs                # Entidade + regras de validação e movimentação de estoque
│   │   ├── ItemCarrinho.cs           # Linha do carrinho (produto + quantidade + subtotal)
│   │   ├── ItemPedido.cs             # Fotografia da linha comprada (congelada no checkout)
│   │   └── Pedido.cs                 # Pedido imutável: itens, valor total e resumo financeiro
│   ├── Services/
│   │   ├── CatalogoProdutos.cs       # Cadastro, consulta, baixa/reposição de estoque e listagem
│   │   ├── CarrinhoDeCompras.cs      # Adição, remoção, limites de estoque e subtotal
│   │   └── ProcessadorDePedidos.cs   # Checkout atômico, numeração e histórico de pedidos
│   ├── Infrastructure/
│   │   └── Formatacao.cs             # Cultura pt-BR centralizada + conversão de entrada
│   └── Program.cs                    # Menu interativo (Etapa 4)
└── MiniEcommerce.Tests/              # Testes unitários (xUnit)
    ├── ProdutoTests.cs
    ├── CatalogoProdutosTests.cs
    ├── ItemCarrinhoTests.cs
    ├── CarrinhoDeComprasTests.cs
    ├── ItemPedidoTests.cs
    ├── PedidoTests.cs
    └── ProcessadorDePedidosTests.cs
```

### Responsabilidade das camadas

| Camada | Namespace | Responsabilidade |
| :--- | :--- | :--- |
| Entidades | `MiniEcommerce.Models` | Regras intrínsecas do domínio: validação de preço/estoque, débito e reposição de estoque, cálculo de totais e imutabilidade do pedido. Não conhecem console nem persistência. |
| Serviços | `MiniEcommerce.Services` | Orquestram as entidades: catálogo (identidade dos produtos, duplicidade, consultas), carrinho (composição de itens e limite de estoque) e processador de pedidos (checkout, baixa de estoque e histórico). |
| Infraestrutura | `MiniEcommerce.Infrastructure` | Apoio transversal sem regra de negócio: cultura `pt-BR` única para formatação de moeda/data e conversão tolerante da entrada digitada. |
| Apresentação | `MiniEcommerce.Console` (`Program.cs`) | Menu interativo, leitura/escrita no terminal e tratamento amigável das exceções de negócio. |
| Testes | `MiniEcommerce.Tests` | xUnit referenciando o projeto principal; uma classe de teste por classe de produção. |

> **Convenção de namespaces:** o projeto de aplicação define `<RootNamespace>MiniEcommerce</RootNamespace>`.
> Isso evita o conflito clássico entre o namespace `MiniEcommerce.Console` e o tipo `System.Console`
> (dentro de `MiniEcommerce.*`, `Console.WriteLine(...)` continua resolvendo para `System.Console`).

### Decisões de projeto

- **`decimal` para valores monetários** (nunca `double`), evitando erros de arredondamento em preço e totais.
- **Exceções como contrato de validação**: `ArgumentException` (nome obrigatório), `ArgumentOutOfRangeException` (preço/estoque/quantidade inválidos) e `InvalidOperationException` (regra de negócio violada, como estoque insuficiente ou nome duplicado).
- **Métodos `Try*`** (`TryCadastrar`, `TryAdicionar`, `TryFinalizar`) para o menu de console exibir mensagens amigáveis sem `try/catch` espalhado pela interface.
- **Encapsulamento**: coleções expostas como `IReadOnlyList<T>`; quantidades e estoque só mudam por métodos com validação.
- **Checkout atômico (tudo-ou-nada)**: `ProcessadorDePedidos.Finalizar` valida **todos** os itens antes de qualquer efeito colateral. Se um único item falhar (produto fora do catálogo ou estoque insuficiente), **nenhum** estoque é debitado, **nenhum** pedido é emitido e o carrinho permanece intacto para correção.
- **Pedido como fotografia imutável**: `ItemPedido` congela nome e preço unitário praticados no momento da compra. Um reajuste posterior via `Produto.AtualizarPreco` não altera pedidos já emitidos — o histórico financeiro permanece auditável.
- **Cultura `pt-BR` centralizada** em `Infrastructure/Formatacao` (`Moeda()`, `DataHora()`, `TentarConverterDecimal()`), eliminando a duplicação que existia em cada classe do domínio.
- **Identificadores sequenciais** controlados pelo catálogo (`_ultimoIdGerado`) e pelo processador de pedidos (`_ultimoNumeroGerado`), sem depender de ordem de inserção externa.

---

## ▶️ Como executar

### Pré-requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior
- Git

### Passos

```bash
# 1. Clonar o repositório
git clone https://github.com/Lebot007/MiniEcommerce.git
cd MiniEcommerce

# 2. Restaurar dependências e compilar a solução
dotnet restore
dotnet build

# 3. Executar os testes unitários
dotnet test

# 4. Executar a aplicação de console
dotnet run --project MiniEcommerce.Console/MiniEcommerce.Console.csproj
```

Comandos úteis:

```bash
dotnet test --verbosity normal          # detalhamento dos testes
dotnet test --logger "console;verbosity=detailed"
dotnet test --collect:"XPlat Code Coverage"   # relatório de cobertura (coverlet)
```

### Menu interativo

Ao executar a aplicação, o menu principal oferece:

| Opção | Ação | Requisito atendido |
| :-: | :--- | :-: |
| **1** | Cadastrar produto (nome, preço e estoque, com validação) | RF 1 |
| **2** | Listar catálogo (id, nome, preço, estoque, esgotados e valor total em estoque) | RF 4 |
| **3** | Adicionar produto ao carrinho (valida estoque disponível) | RF 2 |
| **4** | Ver carrinho (itens, quantidades e subtotal) | RF 2 |
| **5** | Remover item do carrinho (parcialmente ou por completo) | RF 2 |
| **6** | Finalizar pedido — checkout com confirmação, baixa automática de estoque e resumo financeiro | RF 3 |
| **7** | Histórico de pedidos, faturamento e resumo individual por número | RF 3 |
| **8** | Carregar catálogo de demonstração (6 produtos, útil para testar rapidamente) | — |
| **0** | Sair (também aceita `sair`; `Ctrl+D` encerra sem erro) | — |

**Dica — fluxo completo em uma sessão:** `8` (demo) → `3` (adicionar) → `4` (ver carrinho) → `6` + `S` (checkout) → `2` (confirmar a baixa de estoque) → `7` (histórico).

Detalhes de usabilidade:

- Preços aceitam **vírgula ou ponto** decimal: `1.234,56`, `249,90` e `249.90` são compreendidos.
- Entradas inválidas **não derrubam** a aplicação: o programa explica o problema e pede o valor novamente.
- Violações de regra de negócio (preço negativo, estoque insuficiente, nome duplicado, carrinho vazio) aparecem como mensagens `[ERRO]` amigáveis, nunca como *stack trace*.

---

## 🧪 Testes

Os testes são escritos **antes** da implementação (TDD básico, conforme a lista): o commit `test(...)` registra a fase vermelha e o `feat(...)` correspondente a fase verde.

```bash
dotnet test
```

```text
Passed!  - Failed: 0, Passed: 169, Skipped: 0, Total: 169
```

### Cobertura por classe (169 testes)

| Classe de teste | Etapa | Testes | Cenários cobertos |
| :--- | :-: | :-: | :--- |
| `ProdutoTests` | 1 | 32 | atribuição de propriedades; preço negativo, estoque negativo, `id` inválido e nome nulo/em branco rejeitados; fronteira de preço zero e estoque zero; retirada e reposição de estoque; cálculo de total |
| `CatalogoProdutosTests` | 1 | 26 | ids sequenciais; nome duplicado (inclusive com caixa e espaços diferentes); propagação das validações de preço/estoque; `TryCadastrar`; buscas por id e nome (case-insensitive); disponíveis × esgotados; valor total em estoque; remoção; baixa e reposição de estoque; listagem formatada; imutabilidade da coleção |
| `ItemCarrinhoTests` | 2 | 10 | validação de produto nulo e quantidade inválida; subtotal; ajuste de quantidade; `ToString` |
| `CarrinhoDeComprasTests` | 2 | 36 | estado inicial vazio; adição com sucesso; quantidade padrão; quantidade superior ao estoque rejeitada; acumulação do mesmo produto respeitando o estoque; reposição liberando nova adição; subtotal; `Try*`; consulta e remoção (total e parcial); `Limpar`; resumo formatado |
| `ItemPedidoTests` | 3 | 20 | fotografia de id/nome/preço; produto nulo e quantidade inválida; reajuste de preço posterior não altera item emitido; validação de `produtoId`, nome, preço negativo e fronteira de preço zero; `Trim` do nome; subtotal |
| `PedidoTests` | 3 | 16 | atribuição e data do fechamento; número inválido; itens nulos, vazios ou com elemento nulo; aceite de qualquer `IEnumerable`; valor total; formatação em moeda; imutabilidade da coleção; resumo financeiro com número, itens, data e total |
| `ProcessadorDePedidosTests` | 3 | 29 | catálogo nulo; estado inicial; carrinho nulo/vazio; produto fora do catálogo; estoque consumido após a adição; **atomicidade** (falha em um item não debita os demais); mensagem agregando todos os problemas; baixa automática de estoque; esgotamento; limpeza do carrinho; numeração sequencial; congelamento de preço; resumo financeiro; `TryFinalizar`; busca por número; faturamento; histórico formatado; **fluxo completo** integrando os 4 requisitos |

---

## 🔖 Padrão de commits

Histórico 100% em [Conventional Commits](https://www.conventionalcommits.org/pt-br/):

| Prefixo | Uso neste projeto |
| :--- | :--- |
| `chore` | Estrutura inicial da solução e dos projetos |
| `test(escopo)` | Testes unitários escritos antes da implementação (fase vermelha do TDD) |
| `feat(escopo)` | Implementação da regra de negócio coberta pelos testes (fase verde) |
| `refactor` | Melhoria de legibilidade e organização de namespaces sem alterar comportamento |
| `docs` | Documentação (este README) |
| `fix` | Correção de defeito identificado após os testes |

Verificação do histórico:

```bash
git log --oneline
```

---

## 🗺 Roteiro de desenvolvimento da lista

| Etapa | Escopo | Commits | Situação |
| :-: | :--- | :--- | :-: |
| — | Configuração inicial: `sln`, projeto console e projeto xUnit | `chore: initial project structure with console and xunit apps` | ✅ |
| 1 | Regras de negócio e testes de **Produtos** | `test(produto)` → `feat(produto)` | ✅ |
| 2 | **Carrinho de Compras** e regras de adição | `test(carrinho)` → `feat(carrinho)` | ✅ |
| 3 | Processamento de **Pedido** e checkout (baixa de estoque + resumo financeiro) | `test(pedido)` → `feat(pedido)` | ✅ |
| 4 | Interface de console (`Program.cs`) com menu interativo | `feat(console)` | ✅ |
| 5 | Refatoração final e documentação complementar | `refactor(core)` → `docs` | ✅ |

---

## 💳 O fluxo de checkout (Etapa 3)

```text
CarrinhoDeCompras            ProcessadorDePedidos                    Pedido
       │                              │                                │
       │  Finalizar(carrinho)         │                                │
       ├─────────────────────────────>│                                │
       │                              │ 1. carrinho vazio?  -> erro    │
       │                              │ 2. valida TODOS os itens:      │
       │                              │    - consta no catálogo?       │
       │                              │    - há estoque suficiente?    │
       │                              │    (qualquer falha -> erro,    │
       │                              │     nenhum efeito colateral)   │
       │                              │ 3. baixa automática no estoque │
       │                              │ 4. snapshot dos itens          │
       │                              ├───────────────────────────────>│ novo Pedido
       │                              │ 5. registra no histórico       │   (numerado,
       │  carrinho limpo              │                                │    imutável)
       │<─────────────────────────────┤                                │
       └──────────────────────────────┴────────────────────────────────┘
```

Exemplo de resumo financeiro gerado ao final da compra:

```text
Pedido nº 1 - realizado em 31/10/2026 15:56
Produto                         Qtd.     Preço unit.        Subtotal
--------------------------------------------------------------------
Mouse Gamer                        2       R$ 129,90       R$ 259,80
Teclado Mecânico                   2       R$ 249,90       R$ 499,80
--------------------------------------------------------------------
Produtos distintos: 2 | Unidades: 4 | Valor total: R$ 759,60
```

---

## 🔎 Verificação rápida (para o avaliador)

```bash
dotnet build        # 0 avisos, 0 erros
dotnet test         # 169 testes, todos passando
git log --oneline   # histórico aderente ao Conventional Commits
```

---

## 📄 Licença

Repositório criado para fins acadêmicos.
