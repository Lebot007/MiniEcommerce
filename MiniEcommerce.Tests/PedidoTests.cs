using MiniEcommerce.Models;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="Pedido"/> (Etapa 3).
/// Cobrem a imutabilidade do pedido, o cálculo do valor total e a geração do
/// resumo financeiro (requisito funcional 3).
/// </summary>
public class PedidoTests
{
    private static ItemPedido CriarItem(string nome = "Mouse Gamer", decimal preco = 100m, int quantidade = 1, int produtoId = 1) =>
        new(produtoId, nome, preco, quantidade);

    private static Pedido CriarPedidoValido(int numero = 1) =>
        new(numero, new[] { CriarItem(), CriarItem("Teclado Mecânico", 250m, 2, 2) });

    // ------------------------------------------------------------------
    // Construção e validação
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_DadosValidos_PropriedadesSaoAtribuidas()
    {
        var itens = new[] { CriarItem(preco: 100m, quantidade: 2), CriarItem("Teclado", 250m, 1, 2) };
        var realizadoEm = new DateTime(2026, 10, 31, 14, 30, 0);

        var pedido = new Pedido(5, itens, realizadoEm);

        Assert.Equal(5, pedido.Numero);
        Assert.Equal(realizadoEm, pedido.RealizadoEm);
        Assert.Equal(2, pedido.TotalDeProdutos);
        Assert.Equal(3, pedido.QuantidadeTotalDeItens); // 2 + 1
        Assert.Equal(450m, pedido.ValorTotal);          // (2 x 100) + (1 x 250)
    }

    [Fact]
    public void Construtor_SemInformarData_UsaMomentoAtual()
    {
        var antes = DateTime.Now.AddSeconds(-5);

        var pedido = CriarPedidoValido();

        var depois = DateTime.Now.AddSeconds(5);
        Assert.InRange(pedido.RealizadoEm, antes, depois);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_NumeroInvalido_LancaArgumentOutOfRangeException(int numero)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Pedido(numero, new[] { CriarItem() }));

        Assert.Equal("numero", erro.ParamName);
    }

    [Fact]
    public void Construtor_ItensNulos_LancaArgumentNullException()
    {
        var erro = Assert.Throws<ArgumentNullException>(() => new Pedido(1, null!));

        Assert.Equal("itens", erro.ParamName);
    }

    [Fact]
    public void Construtor_ItensVazio_LancaArgumentException()
    {
        var erro = Assert.Throws<ArgumentException>(() => new Pedido(1, Array.Empty<ItemPedido>()));

        Assert.Equal("itens", erro.ParamName);
    }

    [Fact]
    public void Construtor_ItensComElementoNulo_LancaArgumentException()
    {
        var itens = new ItemPedido[] { CriarItem(), null! };

        var erro = Assert.Throws<ArgumentException>(() => new Pedido(1, itens));

        Assert.Equal("itens", erro.ParamName);
    }

    [Fact]
    public void Construtor_AceitaQualquerEnumerableDeItens()
    {
        IEnumerable<ItemPedido> GerarItens()
        {
            yield return CriarItem(preco: 10m, quantidade: 1);
            yield return CriarItem("Cabo HDMI", 25m, 4, 2);
        }

        var pedido = new Pedido(1, GerarItens());

        Assert.Equal(2, pedido.TotalDeProdutos);
        Assert.Equal(110m, pedido.ValorTotal);
    }

    // ------------------------------------------------------------------
    // Valor total
    // ------------------------------------------------------------------

    [Fact]
    public void ValorTotal_SomaSubtotalDeTodosOsItens()
    {
        var pedido = new Pedido(1, new[]
        {
            CriarItem("Produto A", 19.99m, 3),   //  59,97
            CriarItem("Produto B", 5.50m, 4, 2), //  22,00
            CriarItem("Produto C", 120m, 1, 3),  // 120,00
        });

        Assert.Equal(201.97m, pedido.ValorTotal);
    }

    [Fact]
    public void ValorTotal_ItemUnico_CoincideComSubtotal()
    {
        var pedido = new Pedido(1, new[] { CriarItem(preco: 1299m, quantidade: 2) });

        Assert.Equal(2598m, pedido.ValorTotal);
        Assert.Equal(pedido.Itens[0].Subtotal, pedido.ValorTotal);
    }

    [Fact]
    public void ValorTotalFormatado_UsaSimboloDeMoedaBrasileira()
    {
        var pedido = new Pedido(1, new[] { CriarItem(preco: 1234.5m, quantidade: 1) });

        Assert.Contains("R$", pedido.ValorTotalFormatado);
        Assert.Contains("1.234,50", pedido.ValorTotalFormatado);
    }

    // ------------------------------------------------------------------
    // Imutabilidade
    // ------------------------------------------------------------------

    [Fact]
    public void Itens_ColecaoRetornada_NaoPermiteAlteracaoExterna()
    {
        var pedido = CriarPedidoValido();

        Assert.IsAssignableFrom<IReadOnlyList<ItemPedido>>(pedido.Itens);
        Assert.Throws<NotSupportedException>(
            () => ((IList<ItemPedido>)pedido.Itens).Add(CriarItem("Intruso", 1m, 1, 99)));
    }

    [Fact]
    public void Itens_ContemOsMesmosItensInformadosNaCriacao()
    {
        var primeiro = CriarItem("Produto A", 10m, 1);
        var segundo = CriarItem("Produto B", 20m, 2, 2);

        var pedido = new Pedido(1, new[] { primeiro, segundo });

        Assert.Same(primeiro, pedido.Itens[0]);
        Assert.Same(segundo, pedido.Itens[1]);
    }

    // ------------------------------------------------------------------
    // Resumo financeiro
    // ------------------------------------------------------------------

    [Fact]
    public void ObterResumoFormatado_ContemNumeroItensEValorTotal()
    {
        var pedido = new Pedido(42, new[]
        {
            CriarItem("Mouse Gamer", 100m, 2),
            CriarItem("Teclado Mecânico", 250m, 1, 2),
        });

        var resumo = pedido.ObterResumoFormatado();

        Assert.Contains("42", resumo);
        Assert.Contains("Mouse Gamer", resumo);
        Assert.Contains("Teclado Mecânico", resumo);
        Assert.Contains("R$", resumo);
        Assert.Contains("450,00", resumo);
    }

    [Fact]
    public void ObterResumoFormatado_ContemDataDoPedido()
    {
        var pedido = new Pedido(1, new[] { CriarItem() }, new DateTime(2026, 3, 7, 9, 5, 0));

        var resumo = pedido.ObterResumoFormatado();

        Assert.Contains("07/03/2026", resumo);
    }

    [Fact]
    public void ToString_ContemNumeroEValorTotal()
    {
        var pedido = new Pedido(9, new[] { CriarItem(preco: 50m, quantidade: 2) });

        var texto = pedido.ToString();

        Assert.Contains("9", texto);
        Assert.Contains("R$", texto);
        Assert.Contains("100", texto);
    }
}
