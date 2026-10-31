using MiniEcommerce.Models;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="ItemPedido"/> (Etapa 3), a fotografia da linha comprada:
/// garantem que nome e preço praticados no momento do checkout ficam congelados no pedido.
/// </summary>
public class ItemPedidoTests
{
    private static Produto CriarProduto(decimal preco = 100m, int estoque = 10) =>
        new(7, "Headset Gamer", preco, estoque);

    // ------------------------------------------------------------------
    // Construção a partir de um produto
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_APartirDeProduto_CongelaIdNomeEPreco()
    {
        var produto = CriarProduto(preco: 199.90m);

        var item = new ItemPedido(produto, 3);

        Assert.Equal(7, item.ProdutoId);
        Assert.Equal("Headset Gamer", item.NomeProduto);
        Assert.Equal(199.90m, item.PrecoUnitario);
        Assert.Equal(3, item.Quantidade);
    }

    [Fact]
    public void Construtor_APartirDeProdutoNulo_LancaArgumentNullException()
    {
        var erro = Assert.Throws<ArgumentNullException>(() => new ItemPedido(null!, 1));

        Assert.Equal("produto", erro.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Construtor_APartirDeProduto_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => new ItemPedido(CriarProduto(), quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void Construtor_APartirDeProduto_PrecoAlteradoDepois_NaoAfetaItemJaEmitido()
    {
        var produto = CriarProduto(preco: 100m);

        var item = new ItemPedido(produto, 2);
        produto.AtualizarPreco(800m);

        Assert.Equal(100m, item.PrecoUnitario);
        Assert.Equal(200m, item.Subtotal);
    }

    // ------------------------------------------------------------------
    // Construção a partir de valores brutos
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_ValoresBrutos_PropriedadesSaoAtribuidas()
    {
        var item = new ItemPedido(3, "SSD 1TB", 420.50m, 2);

        Assert.Equal(3, item.ProdutoId);
        Assert.Equal("SSD 1TB", item.NomeProduto);
        Assert.Equal(420.50m, item.PrecoUnitario);
        Assert.Equal(2, item.Quantidade);
        Assert.Equal(841.00m, item.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_ValoresBrutos_ProdutoIdInvalido_LancaArgumentOutOfRangeException(int produtoId)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ItemPedido(produtoId, "SSD 1TB", 100m, 1));

        Assert.Equal("produtoId", erro.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ValoresBrutos_NomeNuloOuEmBranco_LancaArgumentException(string? nome)
    {
        var erro = Assert.Throws<ArgumentException>(
            () => new ItemPedido(1, nome!, 100m, 1));

        Assert.Equal("nomeProduto", erro.ParamName);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Construtor_ValoresBrutos_PrecoNegativo_LancaArgumentOutOfRangeException(double preco)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ItemPedido(1, "SSD 1TB", (decimal)preco, 1));

        Assert.Equal("precoUnitario", erro.ParamName);
    }

    [Fact]
    public void Construtor_ValoresBrutos_PrecoZero_EPermitido()
    {
        var item = new ItemPedido(1, "Brinde", 0m, 2);

        Assert.Equal(0m, item.PrecoUnitario);
        Assert.Equal(0m, item.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Construtor_ValoresBrutos_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ItemPedido(1, "SSD 1TB", 100m, quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void Construtor_ValoresBrutos_NomeComEspacos_AplicaTrim()
    {
        var item = new ItemPedido(1, "   SSD 1TB   ", 100m, 1);

        Assert.Equal("SSD 1TB", item.NomeProduto);
    }

    // ------------------------------------------------------------------
    // Cálculos e apresentação
    // ------------------------------------------------------------------

    [Fact]
    public void Subtotal_MultiplicaPrecoUnitarioPelaQuantidade()
    {
        var item = new ItemPedido(1, "Memória RAM 16GB", 19.99m, 3);

        Assert.Equal(59.97m, item.Subtotal);
    }

    [Fact]
    public void ToString_ContemNomeQuantidadeESubtotal()
    {
        var item = new ItemPedido(1, "Memória RAM 16GB", 10m, 2);

        var texto = item.ToString();

        Assert.Contains("Memória RAM 16GB", texto);
        Assert.Contains("2", texto);
        Assert.Contains("20", texto);
    }
}
