using MiniEcommerce.Models;
using MiniEcommerce.Services;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="ProcessadorDePedidos"/> (Etapa 3).
/// Cobrem o fluxo completo de checkout: validação do carrinho, baixa automática e
/// atômica no estoque, cálculo do valor total, numeração sequencial dos pedidos,
/// histórico e resumo financeiro (requisito funcional 3).
/// </summary>
public class ProcessadorDePedidosTests
{
    /// <summary>
    /// Cenário padrão: catálogo com três produtos (1 = Mouse Gamer R$ 100, estoque 10;
    /// 2 = Teclado Mecânico R$ 250, estoque 3; 3 = Webcam Full HD R$ 180, esgotada).
    /// </summary>
    private static (CatalogoProdutos Catalogo, CarrinhoDeCompras Carrinho, ProcessadorDePedidos Processador) CriarCenario()
    {
        var catalogo = new CatalogoProdutos();
        catalogo.Cadastrar("Mouse Gamer", 100m, 10);
        catalogo.Cadastrar("Teclado Mecânico", 250m, 3);
        catalogo.Cadastrar("Webcam Full HD", 180m, 0);

        return (catalogo, new CarrinhoDeCompras(), new ProcessadorDePedidos(catalogo));
    }

    // ------------------------------------------------------------------
    // Construção e estado inicial
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_CatalogoNulo_LancaArgumentNullException()
    {
        var erro = Assert.Throws<ArgumentNullException>(() => new ProcessadorDePedidos(null!));

        Assert.Equal("catalogo", erro.ParamName);
    }

    [Fact]
    public void Construtor_ProcessadorNovo_NasceSemPedidos()
    {
        var (_, _, processador) = CriarCenario();

        Assert.Empty(processador.Pedidos);
        Assert.Equal(0, processador.TotalDePedidos);
        Assert.Equal(1, processador.ProximoNumero);
        Assert.Equal(0m, processador.FaturamentoTotal);
        Assert.Equal(0, processador.QuantidadeTotalDeItensVendidos);
    }

    // ------------------------------------------------------------------
    // Validações do checkout
    // ------------------------------------------------------------------

    [Fact]
    public void Finalizar_CarrinhoNulo_LancaArgumentNullException()
    {
        var (_, _, processador) = CriarCenario();

        var erro = Assert.Throws<ArgumentNullException>(() => processador.Finalizar(null!));

        Assert.Equal("carrinho", erro.ParamName);
    }

    [Fact]
    public void Finalizar_CarrinhoVazio_LancaInvalidOperationException()
    {
        var (_, carrinho, processador) = CriarCenario();

        var erro = Assert.Throws<InvalidOperationException>(() => processador.Finalizar(carrinho));

        Assert.Contains("carrinho está vazio", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, processador.TotalDePedidos);
    }

    [Fact]
    public void Finalizar_ProdutoNaoCadastradoNoCatalogo_LancaInvalidOperationException()
    {
        var (_, carrinho, processador) = CriarCenario();
        var produtoAvulso = new Produto(999, "Produto Avulso", 50m, 5);
        carrinho.Adicionar(produtoAvulso, 1);

        var erro = Assert.Throws<InvalidOperationException>(() => processador.Finalizar(carrinho));

        Assert.Contains("não consta no catálogo", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, processador.TotalDePedidos);
    }

    [Fact]
    public void Finalizar_EstoqueConsumidoAposAdicao_LancaInvalidOperationException()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // usa todo o estoque do teclado
        catalogo.BaixarEstoque(2, 3);                    // venda paralela zera o estoque

        var erro = Assert.Throws<InvalidOperationException>(() => processador.Finalizar(carrinho));

        Assert.Contains("estoque insuficiente", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, processador.TotalDePedidos);
    }

    [Fact]
    public void Finalizar_FalhaEmUmItem_NaoDaBaixaEmNenhumOutro()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 4); // mouse  -> ok
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // teclado -> ficará indisponível
        catalogo.BaixarEstoque(2, 3);                    // zera o estoque do teclado

        Assert.Throws<InvalidOperationException>(() => processador.Finalizar(carrinho));

        // atomicidade: nenhum efeito colateral foi aplicado
        Assert.Equal(10, catalogo.BuscarPorId(1)!.QuantidadeEstoque);
        Assert.Equal(0, catalogo.BuscarPorId(2)!.QuantidadeEstoque);
        Assert.Equal(0, processador.TotalDePedidos);
        Assert.False(carrinho.EstaVazio); // o carrinho é preservado para correção
    }

    [Fact]
    public void Finalizar_VariosItensInvalidos_MensagemListaTodosOsProblemas()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 4);                  // válido no momento da adição
        carrinho.Adicionar(new Produto(888, "Item Fantasma", 1m, 1), 1);  // nunca esteve no catálogo
        catalogo.BaixarEstoque(1, 10);                                    // esgota o mouse antes do checkout

        var erro = Assert.Throws<InvalidOperationException>(() => processador.Finalizar(carrinho));

        Assert.Contains("estoque insuficiente", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("não consta no catálogo", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, processador.TotalDePedidos);
    }

    // ------------------------------------------------------------------
    // Checkout com sucesso
    // ------------------------------------------------------------------

    [Fact]
    public void Finalizar_UmItem_RetornaPedidoNumeroUmComTotalCorreto()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2);

        var pedido = processador.Finalizar(carrinho);

        Assert.NotNull(pedido);
        Assert.Equal(1, pedido.Numero);
        Assert.Single(pedido.Itens);
        Assert.Equal("Mouse Gamer", pedido.Itens[0].NomeProduto);
        Assert.Equal(2, pedido.Itens[0].Quantidade);
        Assert.Equal(200m, pedido.ValorTotal);
    }

    [Fact]
    public void Finalizar_VariosItens_CalculaValorTotalDoPedido()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2); // 2 x 100 = 200
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // 3 x 250 = 750

        var pedido = processador.Finalizar(carrinho);

        Assert.Equal(950m, pedido.ValorTotal);
        Assert.Equal(2, pedido.TotalDeProdutos);
        Assert.Equal(5, pedido.QuantidadeTotalDeItens);
    }

    [Fact]
    public void Finalizar_DaBaixaAutomaticaNoEstoqueDosProdutosAdquiridos()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 4);
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 2);

        processador.Finalizar(carrinho);

        Assert.Equal(6, catalogo.BuscarPorId(1)!.QuantidadeEstoque); // 10 - 4
        Assert.Equal(1, catalogo.BuscarPorId(2)!.QuantidadeEstoque); //  3 - 2
        Assert.Equal(0, catalogo.BuscarPorId(3)!.QuantidadeEstoque); // não comprado
    }

    [Fact]
    public void Finalizar_CompraConsumindoTodoEstoque_DeixaProdutoEsgotado()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3);

        processador.Finalizar(carrinho);

        var teclado = catalogo.BuscarPorId(2)!;
        Assert.Equal(0, teclado.QuantidadeEstoque);
        Assert.False(teclado.EstaDisponivel);
        Assert.Contains(teclado, catalogo.ListarSemEstoque());
    }

    [Fact]
    public void Finalizar_LimpaOCarrinhoAposConfirmacao()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);

        processador.Finalizar(carrinho);

        Assert.True(carrinho.EstaVazio);
        Assert.Equal(0m, carrinho.Subtotal);
    }

    [Fact]
    public void Finalizar_RegistraPedidoNoHistorico()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);

        var pedido = processador.Finalizar(carrinho);

        Assert.Equal(1, processador.TotalDePedidos);
        Assert.Same(pedido, processador.Pedidos[0]);
        Assert.Equal(100m, processador.FaturamentoTotal);
    }

    [Fact]
    public void Finalizar_PedidosSucessivos_NumeraSequencialmente()
    {
        var (catalogo, carrinho, processador) = CriarCenario();

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);
        var primeiro = processador.Finalizar(carrinho);

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);
        var segundo = processador.Finalizar(carrinho);

        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 1);
        var terceiro = processador.Finalizar(carrinho);

        Assert.Equal(1, primeiro.Numero);
        Assert.Equal(2, segundo.Numero);
        Assert.Equal(3, terceiro.Numero);
        Assert.Equal(4, processador.ProximoNumero);
        Assert.Equal(3, processador.TotalDePedidos);
    }

    [Fact]
    public void Finalizar_NaoInterfereNoEstoqueQuandoCarrinhoContinuaUsavel()
    {
        var (catalogo, carrinho, processador) = CriarCenario();

        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // consome todo o estoque do teclado
        processador.Finalizar(carrinho);

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 10); // todo o estoque do mouse
        var segundo = processador.Finalizar(carrinho);

        Assert.Equal(2, segundo.Numero);
        Assert.Equal(0, catalogo.BuscarPorId(1)!.QuantidadeEstoque);
        Assert.Empty(catalogo.ListarDisponiveis());
    }

    [Fact]
    public void Finalizar_CongelaPrecoPraticado_NoMomentoDaCompra()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        var mouse = catalogo.BuscarPorId(1)!;
        carrinho.Adicionar(mouse, 2);

        var pedido = processador.Finalizar(carrinho);
        mouse.AtualizarPreco(150m); // reajuste posterior não altera o pedido emitido

        Assert.Equal(100m, pedido.Itens[0].PrecoUnitario);
        Assert.Equal(200m, pedido.ValorTotal);
        Assert.Equal(200m, processador.FaturamentoTotal);
    }

    [Fact]
    public void Finalizar_ResumoFinanceiroEGeradoCorretamente()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2);
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 1);

        var pedido = processador.Finalizar(carrinho);
        var resumo = pedido.ObterResumoFormatado();

        Assert.Contains("Mouse Gamer", resumo);
        Assert.Contains("Teclado Mecânico", resumo);
        Assert.Contains("R$", resumo);
        Assert.Contains("450,00", resumo); // 200 + 250
    }

    // ------------------------------------------------------------------
    // TryFinalizar
    // ------------------------------------------------------------------

    [Fact]
    public void TryFinalizar_CheckoutValido_RetornaTrueEPedido()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 3);

        var sucesso = processador.TryFinalizar(carrinho, out var pedido, out var erro);

        Assert.True(sucesso);
        Assert.NotNull(pedido);
        Assert.Null(erro);
        Assert.Equal(300m, pedido!.ValorTotal);
        Assert.Equal(7, catalogo.BuscarPorId(1)!.QuantidadeEstoque);
    }

    [Fact]
    public void TryFinalizar_CarrinhoVazio_RetornaFalseComMensagem()
    {
        var (_, carrinho, processador) = CriarCenario();

        var sucesso = processador.TryFinalizar(carrinho, out var pedido, out var erro);

        Assert.False(sucesso);
        Assert.Null(pedido);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Contains("vazio", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryFinalizar_EstoqueInsuficiente_RetornaFalseSemDarBaixa()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3);
        catalogo.BaixarEstoque(2, 1);

        var sucesso = processador.TryFinalizar(carrinho, out var pedido, out var erro);

        Assert.False(sucesso);
        Assert.Null(pedido);
        Assert.Contains("estoque insuficiente", erro, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, catalogo.BuscarPorId(2)!.QuantidadeEstoque);
        Assert.Equal(0, processador.TotalDePedidos);
    }

    [Fact]
    public void TryFinalizar_CarrinhoNulo_RetornaFalseSemLancarExcecao()
    {
        var (_, _, processador) = CriarCenario();

        var sucesso = processador.TryFinalizar(null!, out var pedido, out var erro);

        Assert.False(sucesso);
        Assert.Null(pedido);
        Assert.False(string.IsNullOrWhiteSpace(erro));
    }

    // ------------------------------------------------------------------
    // Consulta e histórico
    // ------------------------------------------------------------------

    [Fact]
    public void BuscarPorNumero_PedidoExistente_RetornaPedido()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);
        var emitido = processador.Finalizar(carrinho);

        var encontrado = processador.BuscarPorNumero(1);

        Assert.NotNull(encontrado);
        Assert.Same(emitido, encontrado);
    }

    [Fact]
    public void BuscarPorNumero_PedidoInexistente_RetornaNull()
    {
        var (_, _, processador) = CriarCenario();

        Assert.Null(processador.BuscarPorNumero(123));
    }

    [Fact]
    public void FaturamentoTotal_SomaValorDeTodosOsPedidos()
    {
        var (catalogo, carrinho, processador) = CriarCenario();

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2); // 200
        processador.Finalizar(carrinho);

        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // 750
        processador.Finalizar(carrinho);

        Assert.Equal(950m, processador.FaturamentoTotal);
        Assert.Equal(5, processador.QuantidadeTotalDeItensVendidos);
    }

    [Fact]
    public void Pedidos_ColecaoRetornada_NaoPermiteAlteracaoExterna()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);
        processador.Finalizar(carrinho);

        Assert.IsAssignableFrom<IReadOnlyList<Pedido>>(processador.Pedidos);
        Assert.Throws<NotSupportedException>(
            () => ((IList<Pedido>)processador.Pedidos).Add(new Pedido(99, new[] { new ItemPedido(1, "X", 1m, 1) })));
    }

    [Fact]
    public void ObterHistoricoFormatado_SemPedidos_ExibeMensagemInformativa()
    {
        var (_, _, processador) = CriarCenario();

        Assert.Contains("nenhum pedido", processador.ObterHistoricoFormatado(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObterHistoricoFormatado_ComPedidos_ContemNumerosValoresEFaturamento()
    {
        var (catalogo, carrinho, processador) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2);
        processador.Finalizar(carrinho);
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 1);
        processador.Finalizar(carrinho);

        var historico = processador.ObterHistoricoFormatado();

        Assert.Contains("1", historico);
        Assert.Contains("2", historico);
        Assert.Contains("R$", historico);
        Assert.Contains("200,00", historico);
        Assert.Contains("250,00", historico);
        Assert.Contains("450,00", historico); // faturamento acumulado
    }

    // ------------------------------------------------------------------
    // Fluxo completo (requisitos funcionais 1 a 4 integrados)
    // ------------------------------------------------------------------

    [Fact]
    public void FluxoCompleto_CadastroAdicaoAoCarrinhoECheckout()
    {
        var catalogo = new CatalogoProdutos();
        var carrinho = new CarrinhoDeCompras();
        var processador = new ProcessadorDePedidos(catalogo);

        // 1. cadastro com validação
        Assert.True(catalogo.TryCadastrar("Notebook", 3500m, 4, out var notebook, out _));
        Assert.True(catalogo.TryCadastrar("Memória RAM 16GB", 320m, 12, out var memoria, out _));
        Assert.False(catalogo.TryCadastrar("Preço Inválido", -1m, 3, out _, out _));

        // 2. carrinho respeitando o limite de estoque
        carrinho.Adicionar(notebook!, 2);
        carrinho.Adicionar(memoria!, 4);
        Assert.False(carrinho.TryAdicionar(notebook!, 99, out _)); // tentativa acima do estoque
        Assert.Equal(8280m, carrinho.Subtotal);                    // (2 x 3500) + (4 x 320)

        // 3. checkout com baixa automática de estoque
        var pedido = processador.Finalizar(carrinho);

        Assert.Equal(1, pedido.Numero);
        Assert.Equal(2, notebook!.QuantidadeEstoque);  // 4 - 2
        Assert.Equal(8, memoria!.QuantidadeEstoque);   // 12 - 4
        Assert.True(carrinho.EstaVazio);

        // 4. valor total calculado a partir dos itens selecionados
        Assert.Equal((2 * 3500m) + (4 * 320m), pedido.ValorTotal);
        Assert.Equal(8280m, pedido.ValorTotal);
        Assert.Equal(pedido.ValorTotal, processador.FaturamentoTotal);    }
}
