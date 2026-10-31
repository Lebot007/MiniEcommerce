# MiniEcommerce

> **Lista de Exercícios VI** — Garantia da Qualidade de Software / Gestão e Qualidade de Software
> Professor: **Daniel Henrique Matos de Paiva**

Mini e-commerce de linha de comando desenvolvido em **.NET 8 (C#)**, com **TDD** (testes unitários em **xUnit**) e histórico Git rigorosamente aderente ao padrão **Conventional Commits**.

---

## 👥 Equipe

| Papel | Nome | RA | GitHub |
| :--- | :--- | :--- | :--- |
| Desenvolvedor 1 | João Vitor Alves Rodrigues | 32513480 | [@Lebot007](https://github.com/Lebot007) |
| Desenvolvedor 2 | *a preencher* | *a preencher* | *a preencher* |

---

## 📋 Requisitos funcionais

| # | Requisito | Situação |
| :-: | :--- | :--- |
| 1 | **Cadastro de Produtos** — nome, preço e quantidade em estoque, com validação que impede preço negativo ou estoque menor que zero | ✅ Implementado (`Produto`, `CatalogoProdutos`) |
| 2 | **Gerenciamento de Carrinho de Compras** — adicionar produtos informando a quantidade, validar estoque disponível e remover itens | ✅ Implementado (`CarrinhoDeCompras`, `ItemCarrinho`) |
| 3 | **Fechamento de Pedido (Checkout)** — processar a compra, dar baixa automática no estoque e calcular o valor total do pedido | ⏳ Em desenvolvimento (Etapa 3) |
| 4 | **Catálogo e Consulta** — exibir a lista de produtos com preços e estoques atuais | ✅ Regra pronta (`CatalogoProdutos.ObterListagemFormatada`) — exibição no menu na Etapa 4 |

---

## 🏗 Arquitetura

Solução composta por dois projetos, conforme proposto na lista:

```text
MiniEcommerce/
├── MiniEcommerce.sln
├── MiniEcommerce.Console/            # Aplicação (biblioteca de domínio + entrada console)
│   ├── Models/
│   │   ├── Produto.cs                # Entidade + regras de validação e movimentação de estoque
│   │   └── ItemCarrinho.cs           # Linha do carrinho (produto + quantidade + subtotal)
│   ├── Services/
│   │   ├── CatalogoProdutos.cs       # Cadastro, consulta, baixa/reposição de estoque e listagem
│   │   └── CarrinhoDeCompras.cs      # Adição, remoção, limites de estoque e subtotal
│   └── Program.cs                    # Menu interativo (Etapa 4)
└── MiniEcommerce.Tests/              # Testes unitários (xUnit)
    ├── ProdutoTests.cs
    ├── CatalogoProdutosTests.cs
    ├── ItemCarrinhoTests.cs
    └── CarrinhoDeComprasTests.cs
```

### Responsabilidade das camadas

| Camada | Namespace | Responsabilidade |
| :--- | :--- | :--- |
| Entidades | `MiniEcommerce.Models` | Regras intrínsecas do domínio: validação de preço/estoque, débito e reposição de estoque, cálculo de totais. Não conhecem console nem persistência. |
| Serviços | `MiniEcommerce.Services` | Orquestram as entidades: catálogo (identidade dos produtos, duplicidade, consultas) e carrinho (composição de itens e validação de limite de estoque). |
| Apresentação | `MiniEcommerce.Console` (`Program.cs`) | Menu interativo, leitura/escrita no terminal e tratamento amigável das exceções de negócio. |
| Testes | `MiniEcommerce.Tests` | xUnit referenciando o projeto principal; uma classe de teste por classe de produção. |

> **Convenção de namespaces:** o projeto de aplicação define `<RootNamespace>MiniEcommerce</RootNamespace>`.
> Isso evita o conflito clássico entre o namespace `MiniEcommerce.Console` e o tipo `System.Console`
> (dentro de `MiniEcommerce.*`, `Console.WriteLine(...)` continua resolvendo para `System.Console`).

### Decisões de projeto

- **`decimal` para valores monetários** (nunca `double`), evitando erros de arredondamento em preço e totais.
- **Exceções como contrato de validação**: `ArgumentException` (nome obrigatório), `ArgumentOutOfRangeException` (preço/estoque/quantidade inválidos) e `InvalidOperationException` (regra de negócio violada, como estoque insuficiente ou nome duplicado).
- **Métodos `Try*`** (`TryCadastrar`, `TryAdicionar`) para o menu de console exibir mensagens amigáveis sem `try/catch` espalhado pela interface.
- **Encapsulamento**: coleções expostas como `IReadOnlyList<T>`; quantidades e estoque só mudam por métodos com validação.
- **Cultura `pt-BR`** na formatação de valores (`R$ 1.234,56`) nas listagens de console.
- **Identificadores sequenciais** controlados pelo catálogo (`_ultimoIdGerado`), sem depender de ordem de inserção externa.

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

---

## 🧪 Testes

Os testes são escritos **antes** da implementação (TDD básico, conforme a lista): o commit `test(...)` registra a fase vermelha e o `feat(...)` correspondente a fase verde.

```bash
dotnet test
```

Cenários cobertos atualmente (**104 testes**):

- **`Produto`** — atribuição de propriedades; preço negativo, estoque negativo, `id` inválido e nome nulo/em branco rejeitados; fronteira de preço zero e estoque zero; retirada e reposição de estoque; cálculo de total.
- **`CatalogoProdutos`** — ids sequenciais; nome duplicado (inclusive com caixa e espaços diferentes); propagação das validações de preço/estoque; `TryCadastrar`; buscas por id e nome (case-insensitive); produtos disponíveis × esgotados; valor total em estoque; remoção; baixa e reposição de estoque; listagem formatada.
- **`ItemCarrinho`** — validação de produto nulo e quantidade inválida; subtotal; ajuste de quantidade.
- **`CarrinhoDeCompras`** — estado inicial vazio; adição com sucesso; quantidade padrão; quantidade superior ao estoque rejeitada; acumulação do mesmo produto respeitando o estoque; reposição de estoque liberando nova adição; subtotal; `Try*`; consulta e remoção (total e parcial); `Limpar`; resumo formatado.

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
| 3 | Processamento de **Pedido** e checkout (baixa de estoque + resumo financeiro) | `test(pedido)` → `feat(pedido)` | ⏳ |
| 4 | Interface de console (`Program.cs`) com menu interativo | `feat(console)` | ⏳ |
| 5 | Refatoração final e documentação complementar | `refactor(core)` → `docs` | ⏳ |

### Pontos de integração já preparados para a Etapa 3 (Checkout)

A base de domínio já expõe tudo o que o fechamento de pedido precisa:

- `Produto.RetirarDoEstoque(int)` → `bool` — débito atômico: só altera o estoque se houver unidades suficientes.
- `CatalogoProdutos.BaixarEstoque(int produtoId, int quantidade)` → `bool` — baixa por produto a partir do catálogo.
- `CarrinhoDeCompras.Itens`, `.Subtotal` e `.Limpar()` — leitura dos itens comprados, valor total e esvaziamento após a confirmação.

---

## 📄 Licença

Repositório criado para fins acadêmicos.
