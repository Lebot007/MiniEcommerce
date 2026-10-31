using System.Globalization;
using MiniEcommerce.Infrastructure;
using MiniEcommerce.Models;
using MiniEcommerce.Services;

// ============================================================================
//  MiniEcommerce — Etapa 4: interface de console
//
//  Menu interativo que exercita os quatro requisitos funcionais da lista:
//    1. Cadastro de produtos, com validação de preço e de estoque
//    2. Gerenciamento do carrinho: adicionar, remover e respeitar o estoque
//    3. Fechamento de pedido (checkout): baixa automática e valor total
//    4. Catálogo e consulta: produtos, preços e estoques atuais
//
//  Nenhuma regra de negócio mora aqui: este arquivo cuida apenas de apresentação
//  (leitura, escrita e navegação). Produto, ItemCarrinho, Pedido, CatalogoProdutos,
//  CarrinhoDeCompras e ProcessadorDePedidos concentram o domínio e são cobertos
//  por testes unitários no projeto MiniEcommerce.Tests.
// ============================================================================

// Fixa a cultura da aplicação para que valores e datas saiam sempre no padrão
// brasileiro (R$ 1.234,56 e dd/MM/aaaa), independentemente do sistema operacional.
CultureInfo.DefaultThreadCurrentCulture = Formatacao.CulturaBrasileira;
CultureInfo.DefaultThreadCurrentUICulture = Formatacao.CulturaBrasileira;

var catalogo = new CatalogoProdutos();
var carrinho = new CarrinhoDeCompras();
var processador = new ProcessadorDePedidos(catalogo);

ExibirAbertura();

try
{
    while (true)
    {
        ExibirMenu();

        var entrada = Console.ReadLine();

        if (entrada is null) // EOF (Ctrl+D ou entrada redirecionada): encerra sem quebrar
        {
            Console.WriteLine();
            EscreverInformacao("Entrada encerrada. Até a próxima!");
            break;
        }

        var opcao = entrada.Trim();

        if (IsOpcaoSair(opcao))
        {
            EscreverInformacao("Encerrando o MiniEcommerce. Obrigado por comprar conosco!");
            break;
        }

        switch (opcao)
        {
            case "1": CadastrarProduto(); break;
            case "2": ListarCatalogo(); break;
            case "3": AdicionarAoCarrinho(); break;
            case "4": VerCarrinho(); break;
            case "5": RemoverDoCarrinho(); break;
            case "6": FinalizarPedido(); break;
            case "7": VerHistoricoDePedidos(); break;
            case "8": CarregarCatalogoDeDemonstracao(); break;

            default:
                EscreverErro($"Opção inválida: '{opcao}'. Digite um número entre 0 e 8.");
                break;
        }

        Pausar();
    }
}
catch (Exception ex)
{
    EscreverErro($"Erro inesperado: {ex.Message}");
    return 1;
}

return 0;

// ----------------------------------------------------------------------------
// Apresentação
// ----------------------------------------------------------------------------

void ExibirAbertura()
{
    Console.WriteLine("==============================================================");
    Console.WriteLine("        M I N I E C O M M E R C E  •  L O J A  C L I");
    Console.WriteLine("==============================================================");
    Console.WriteLine(" Garantia da Qualidade de Software — .NET 8 + xUnit (TDD)");
    Console.WriteLine(" Domínio testado: produto, catálogo, carrinho e pedido.");
}

void ExibirMenu()
{
    Console.WriteLine();
    EscreverTitulo("MENU PRINCIPAL");
    Console.WriteLine($"  Catálogo: {catalogo.TotalDeProdutos} produto(s)  |  " +
                      $"Carrinho: {carrinho.QuantidadeTotalDeItens} unidade(s)  |  " +
                      $"Pedidos: {processador.TotalDePedidos}");
    Console.WriteLine(new string('-', 62));
    Console.WriteLine("  1 - Cadastrar produto");
    Console.WriteLine("  2 - Listar catálogo");
    Console.WriteLine("  3 - Adicionar produto ao carrinho");
    Console.WriteLine("  4 - Ver carrinho");
    Console.WriteLine("  5 - Remover item do carrinho");
    Console.WriteLine("  6 - Finalizar pedido (checkout)");
    Console.WriteLine("  7 - Histórico de pedidos");
    Console.WriteLine("  8 - Carregar catálogo de demonstração");
    Console.WriteLine("  0 - Sair");
    Console.WriteLine(new string('-', 62));
}

void EscreverTitulo(string titulo)
{
    Console.WriteLine();
    Console.WriteLine($"=== {titulo} ===");
}

void EscreverSucesso(string mensagem) => EscreverColorido($"[OK]    {mensagem}", ConsoleColor.Green);

void EscreverErro(string mensagem) => EscreverColorido($"[ERRO]  {mensagem}", ConsoleColor.Red);

void EscreverInformacao(string mensagem) => EscreverColorido($"[INFO]  {mensagem}", ConsoleColor.Cyan);

void EscreverColorido(string mensagem, ConsoleColor cor)
{
    try
    {
        var corOriginal = Console.ForegroundColor;
        Console.ForegroundColor = cor;
        Console.WriteLine(mensagem);
        Console.ForegroundColor = corOriginal;
    }
    catch (Exception)
    {
        // Terminais sem suporte a cor (ou saída redirecionada) seguem funcionando.
        Console.WriteLine(mensagem);
    }
}

void Pausar()
{
    Console.WriteLine();
    Console.Write("Pressione ENTER para voltar ao menu...");
    Console.ReadLine();
}

static bool IsOpcaoSair(string opcao) =>
    string.Equals(opcao, "0", StringComparison.Ordinal) ||
    string.Equals(opcao, "sair", StringComparison.OrdinalIgnoreCase);

// ----------------------------------------------------------------------------
// Ações do menu
// ----------------------------------------------------------------------------

void CadastrarProduto()
{
    EscreverTitulo("CADASTRO DE PRODUTO (requisito funcional 1)");
    Console.WriteLine("Preço negativo ou estoque menor que zero são recusados pela regra de negócio.");

    if (!LerTexto("Nome do produto: ", out var nome)) return;
    if (!LerPreco("Preço unitário (ex.: 249,90): ", out var preco)) return;
    if (!LerInteiro("Quantidade em estoque: ", out var estoque)) return;

    if (catalogo.TryCadastrar(nome, preco, estoque, out var produto, out var erro))
    {
        EscreverSucesso($"Produto cadastrado com o código {produto!.Id} — {produto}");
        return;
    }

    EscreverErro(erro!);
}

void ListarCatalogo()
{
    EscreverTitulo("CATÁLOGO DE PRODUTOS (requisito funcional 4)");
    Console.WriteLine(catalogo.ObterListagemFormatada());
    Console.WriteLine($"Disponíveis: {catalogo.ListarDisponiveis().Count()}  |  " +
                      $"Esgotados: {catalogo.ListarSemEstoque().Count()}");
}

void AdicionarAoCarrinho()
{
    EscreverTitulo("ADICIONAR AO CARRINHO (requisito funcional 2)");

    if (catalogo.TotalDeProdutos == 0)
    {
        EscreverErro("Nenhum produto cadastrado. Use a opção 1 (ou a opção 8) antes de montar o carrinho.");
        return;
    }

    Console.WriteLine(catalogo.ObterListagemFormatada());

    if (!LerInteiro("Código do produto: ", out var codigo)) return;

    var produto = catalogo.BuscarPorId(codigo);

    if (produto is null)
    {
        EscreverErro($"Não existe produto com o código {codigo} no catálogo.");
        return;
    }

    if (!produto.EstaDisponivel)
    {
        EscreverErro($"O produto '{produto.Nome}' está esgotado (estoque zero).");
        return;
    }

    if (!LerInteiro($"Quantidade desejada (estoque disponível: {produto.QuantidadeEstoque}): ", out var quantidade)) return;

    if (carrinho.TryAdicionar(produto, quantidade, out var erro))
    {
        EscreverSucesso($"{quantidade} unidade(s) de '{produto.Nome}' adicionada(s) ao carrinho.");
        Console.WriteLine($"       Subtotal do carrinho: {carrinho.Subtotal.Moeda()}");
        return;
    }

    EscreverErro(erro!);
}

void VerCarrinho()
{
    EscreverTitulo("CARRINHO DE COMPRAS");
    Console.WriteLine(carrinho.ObterResumoFormatado());
}

void RemoverDoCarrinho()
{
    EscreverTitulo("REMOVER ITEM DO CARRINHO");

    if (carrinho.EstaVazio)
    {
        EscreverInformacao("O carrinho está vazio — não há itens para remover.");
        return;
    }

    Console.WriteLine(carrinho.ObterResumoFormatado());

    if (!LerInteiro("Código do produto a remover: ", out var codigo)) return;

    var item = carrinho.ObterItem(codigo);

    if (item is null)
    {
        EscreverErro($"O produto de código {codigo} não está no carrinho.");
        return;
    }

    Console.WriteLine($"  1 - Remover algumas unidades (há {item.Quantidade} no carrinho)");
    Console.WriteLine("  2 - Remover o item inteiro");

    if (!LerInteiro("Como deseja remover? ", out var modo)) return;

    if (modo == 2)
    {
        if (carrinho.Remover(codigo))
        {
            EscreverSucesso($"'{item.Produto.Nome}' removido do carrinho.");
        }

        return;
    }

    if (modo != 1)
    {
        EscreverErro($"Opção inválida: {modo}. Nenhuma alteração foi feita.");
        return;
    }

    if (!LerInteiro("Quantidade a remover: ", out var quantidade)) return;

    if (quantidade <= 0)
    {
        EscreverErro("A quantidade a remover deve ser maior que zero.");
        return;
    }

    if (carrinho.RemoverQuantidade(codigo, quantidade))
    {
        EscreverSucesso($"{quantidade} unidade(s) de '{item.Produto.Nome}' removida(s) do carrinho.");
        return;
    }

    EscreverErro($"Não é possível remover {quantidade} unidade(s): o carrinho tem apenas {item.Quantidade}.");
}

void FinalizarPedido()
{
    EscreverTitulo("FECHAMENTO DE PEDIDO — CHECKOUT (requisito funcional 3)");

    if (carrinho.EstaVazio)
    {
        EscreverErro("O carrinho está vazio. Adicione produtos (opção 3) antes de finalizar o pedido.");
        return;
    }

    Console.WriteLine(carrinho.ObterResumoFormatado());
    Console.WriteLine($"Valor a pagar: {carrinho.Subtotal.Moeda()}");
    Console.WriteLine();

    if (!Confirmar("Confirmar a compra e dar baixa automática no estoque? (S/N): "))
    {
        EscreverInformacao("Checkout cancelado. O carrinho foi mantido.");
        return;
    }

    if (!processador.TryFinalizar(carrinho, out var pedido, out var erro))
    {
        EscreverErro(erro!);
        return;
    }

    EscreverSucesso("Compra confirmada! O estoque dos produtos adquiridos foi atualizado.");
    Console.WriteLine();
    Console.WriteLine(pedido!.ObterResumoFormatado());
}

void VerHistoricoDePedidos()
{
    EscreverTitulo("HISTÓRICO DE PEDIDOS");
    Console.WriteLine(processador.ObterHistoricoFormatado());

    if (processador.TotalDePedidos == 0)
    {
        return;
    }

    if (!LerInteiroOpcional("Número do pedido para ver o resumo financeiro (ENTER para voltar): ", out var numero)) return;

    if (numero is null)
    {
        return;
    }

    var pedido = processador.BuscarPorNumero(numero.Value);

    if (pedido is null)
    {
        EscreverErro($"Pedido nº {numero.Value} não encontrado.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine(pedido.ObterResumoFormatado());
}

void CarregarCatalogoDeDemonstracao()
{
    EscreverTitulo("CATÁLOGO DE DEMONSTRAÇÃO");

    (string Nome, decimal Preco, int Estoque)[] exemplos =
    {
        ("Teclado Mecânico",    249.90m, 10),
        ("Mouse Gamer",         129.90m, 15),
        ("Headset RGB",         359.00m,  7),
        ("Monitor 27\" 144Hz", 1299.00m,  4),
        ("Webcam Full HD",      199.00m,  0),
        ("Cabo HDMI 2m",         49.90m, 30),
    };

    var incluidos = 0;

    foreach (var (nome, preco, estoque) in exemplos)
    {
        if (catalogo.TryCadastrar(nome, preco, estoque, out var produto, out _))
        {
            incluidos++;
            EscreverSucesso($"código {produto!.Id,-3} {produto}");
        }
    }

    if (incluidos == 0)
    {
        EscreverInformacao("Todos os produtos de demonstração já estavam cadastrados.");
        return;
    }

    EscreverInformacao($"{incluidos} produto(s) de demonstração cadastrado(s).");
}

// ----------------------------------------------------------------------------
// Leitura e validação de entrada
// ----------------------------------------------------------------------------

bool LerTexto(string rotulo, out string valor)
{
    while (true)
    {
        Console.Write(rotulo);

        var linha = Console.ReadLine();

        if (linha is null)
        {
            valor = string.Empty;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(linha))
        {
            valor = linha.Trim();
            return true;
        }

        EscreverErro("Valor obrigatório. Tente novamente.");
    }
}

bool LerPreco(string rotulo, out decimal valor)
{
    while (true)
    {
        Console.Write(rotulo);

        var linha = Console.ReadLine();

        if (linha is null)
        {
            valor = 0m;
            return false;
        }

        if (Formatacao.TentarConverterDecimal(linha, out valor))
        {
            return true;
        }

        EscreverErro("Preço inválido. Use vírgula ou ponto decimal — por exemplo: 249,90");
    }
}

bool LerInteiro(string rotulo, out int valor)
{
    while (true)
    {
        Console.Write(rotulo);

        var linha = Console.ReadLine();

        if (linha is null)
        {
            valor = 0;
            return false;
        }

        if (Formatacao.TentarConverterInteiro(linha, out valor))
        {
            return true;
        }

        EscreverErro("Número inválido. Informe um inteiro — por exemplo: 3");
    }
}

bool LerInteiroOpcional(string rotulo, out int? valor)
{
    while (true)
    {
        Console.Write(rotulo);

        var linha = Console.ReadLine();

        if (linha is null)
        {
            valor = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(linha))
        {
            valor = null;
            return true;
        }

        if (Formatacao.TentarConverterInteiro(linha, out var numero))
        {
            valor = numero;
            return true;
        }

        EscreverErro("Número inválido. Informe um inteiro ou pressione ENTER para voltar.");
    }
}

bool Confirmar(string rotulo)
{
    Console.Write(rotulo);

    var linha = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(linha))
    {
        return false;
    }

    return linha.Trim().ToLowerInvariant() is "s" or "sim" or "y" or "yes";
}
