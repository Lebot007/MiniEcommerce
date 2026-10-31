using MiniEcommerce.Models;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="ItemCarrinho"/> (Etapa 2),
/// o par produto + quantidade mantido dentro do carrinho.
/// </summary>
public class ItemCarrinhoTests
{
    private static Produto CriarProduto(decimal preco = 50m, int estoque = 10) =>
        new(1, "Mouse Gamer", preco, estoque);

    [Fact]
    public void Construtor_DadosValidos_PropriedadesSaoAtribuidas()
    {
        var produto = CriarProduto();

        var item = new ItemCarrinho(produto, 3);

        Assert.Same(produto, item.Produto);
        Assert.Equal(3, item.Quantidade);
    }

    [Fact]
    public void Construtor_ProdutoNulo_LancaArgumentNullException()
    {
        var erro = Assert.Throws<ArgumentNullException>(() => new ItemCarrinho(null!, 1));

        Assert.Equal("produto", erro.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-25)]
    public void Construtor_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => new ItemCarrinho(CriarProduto(), quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void Subtotal_MultiplicaPrecoPelaQuantidade()
    {
        var item = new ItemCarrinho(CriarProduto(preco: 19.99m), 3);

        Assert.Equal(59.97m, item.Subtotal);
    }

    [Fact]
    public void AjustarQuantidade_ValorValido_AtualizaQuantidadeESubtotal()
    {
        var item = new ItemCarrinho(CriarProduto(preco: 10m), 2);

        item.AjustarQuantidade(5);

        Assert.Equal(5, item.Quantidade);
        Assert.Equal(50m, item.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AjustarQuantidade_ValorInvalido_LancaArgumentOutOfRangeException(int quantidade)
    {
        var item = new ItemCarrinho(CriarProduto(), 2);

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => item.AjustarQuantidade(quantidade));

        Assert.Equal("quantidade", erro.ParamName);
        Assert.Equal(2, item.Quantidade);
    }

    [Fact]
    public void ToString_ContemNomeQuantidadeESubtotal()
    {
        var item = new ItemCarrinho(CriarProduto(preco: 10m), 2);

        var texto = item.ToString();

        Assert.Contains("Mouse Gamer", texto);
        Assert.Contains("2", texto);
        Assert.Contains("20", texto);
    }
}
