using MiniEcommerce.Models;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários da entidade <see cref="Produto"/> (Etapa 1).
/// Garantem o requisito funcional 1: cadastro com validação que impede
/// preços negativos e estoque menor que zero.
/// </summary>
public class ProdutoTests
{
    private const int IdValido = 1;
    private const string NomeValido = "Teclado Mecânico";
    private const decimal PrecoValido = 249.90m;
    private const int EstoqueValido = 10;

    private static Produto CriarProdutoValido(decimal preco = PrecoValido, int estoque = EstoqueValido) =>
        new(IdValido, NomeValido, preco, estoque);

    public static TheoryData<decimal> PrecosNegativos => new() { -0.01m, -1m, -1000m };

    public static TheoryData<int> EstoquesNegativos => new() { -1, -10, -999 };

    public static TheoryData<int> QuantidadesInvalidas => new() { 0, -1, -50 };

    // ------------------------------------------------------------------
    // Construção e validação
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_DadosValidos_PropriedadesSaoAtribuidas()
    {
        var produto = CriarProdutoValido();

        Assert.Equal(IdValido, produto.Id);
        Assert.Equal(NomeValido, produto.Nome);
        Assert.Equal(PrecoValido, produto.Preco);
        Assert.Equal(EstoqueValido, produto.QuantidadeEstoque);
    }

    [Theory]
    [MemberData(nameof(PrecosNegativos))]
    public void Construtor_PrecoNegativo_LancaArgumentOutOfRangeException(decimal preco)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Produto(IdValido, NomeValido, preco, EstoqueValido));

        Assert.Equal("preco", erro.ParamName);
    }

    [Theory]
    [MemberData(nameof(EstoquesNegativos))]
    public void Construtor_EstoqueNegativo_LancaArgumentOutOfRangeException(int estoque)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Produto(IdValido, NomeValido, PrecoValido, estoque));

        Assert.Equal("quantidadeEstoque", erro.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_IdInvalido_LancaArgumentOutOfRangeException(int id)
    {
        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Produto(id, NomeValido, PrecoValido, EstoqueValido));

        Assert.Equal("id", erro.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_NomeNuloOuEmBranco_LancaArgumentException(string? nome)
    {
        var erro = Assert.Throws<ArgumentException>(
            () => new Produto(IdValido, nome!, PrecoValido, EstoqueValido));

        Assert.Equal("nome", erro.ParamName);
    }

    [Fact]
    public void Construtor_PrecoZero_EPermitido()
    {
        var produto = CriarProdutoValido(preco: 0m);

        Assert.Equal(0m, produto.Preco);
    }

    [Fact]
    public void Construtor_EstoqueZero_EPermitido()
    {
        var produto = CriarProdutoValido(estoque: 0);

        Assert.Equal(0, produto.QuantidadeEstoque);
    }

    // ------------------------------------------------------------------
    // Controle de estoque
    // ------------------------------------------------------------------

    [Fact]
    public void TemEstoqueSuficiente_QuantidadeIgualAoEstoque_RetornaTrue()
    {
        var produto = CriarProdutoValido(estoque: 5);

        Assert.True(produto.TemEstoqueSuficiente(5));
    }

    [Fact]
    public void TemEstoqueSuficiente_QuantidadeMaiorQueEstoque_RetornaFalse()
    {
        var produto = CriarProdutoValido(estoque: 5);

        Assert.False(produto.TemEstoqueSuficiente(6));
    }

    [Theory]
    [MemberData(nameof(QuantidadesInvalidas))]
    public void TemEstoqueSuficiente_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var produto = CriarProdutoValido();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => produto.TemEstoqueSuficiente(quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void RetirarDoEstoque_EstoqueSuficiente_DebitaQuantidadeERetornaTrue()
    {
        var produto = CriarProdutoValido(estoque: 10);

        var resultado = produto.RetirarDoEstoque(4);

        Assert.True(resultado);
        Assert.Equal(6, produto.QuantidadeEstoque);
    }

    [Fact]
    public void RetirarDoEstoque_EstoqueInsuficiente_RetornaFalseENaoAlteraEstoque()
    {
        var produto = CriarProdutoValido(estoque: 3);

        var resultado = produto.RetirarDoEstoque(4);

        Assert.False(resultado);
        Assert.Equal(3, produto.QuantidadeEstoque);
    }

    [Fact]
    public void RetirarDoEstoque_EstoqueZerado_RetornaFalse()
    {
        var produto = CriarProdutoValido(estoque: 0);

        Assert.False(produto.RetirarDoEstoque(1));
    }

    [Theory]
    [MemberData(nameof(QuantidadesInvalidas))]
    public void RetirarDoEstoque_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var produto = CriarProdutoValido();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => produto.RetirarDoEstoque(quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void ReporEstoque_QuantidadeValida_SomaAoEstoqueAtual()
    {
        var produto = CriarProdutoValido(estoque: 2);

        produto.ReporEstoque(8);

        Assert.Equal(10, produto.QuantidadeEstoque);
    }

    [Theory]
    [MemberData(nameof(QuantidadesInvalidas))]
    public void ReporEstoque_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var produto = CriarProdutoValido();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => produto.ReporEstoque(quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    // ------------------------------------------------------------------
    // Cálculo de valores
    // ------------------------------------------------------------------

    [Fact]
    public void CalcularTotal_QuantidadeValida_MultiplicaPrecoPorQuantidade()
    {
        var produto = CriarProdutoValido(preco: 19.99m);

        Assert.Equal(59.97m, produto.CalcularTotal(3));
    }

    [Fact]
    public void CalcularTotal_QuantidadeZero_RetornaZero()
    {
        var produto = CriarProdutoValido(preco: 19.99m);

        Assert.Equal(0m, produto.CalcularTotal(0));
    }

    [Fact]
    public void CalcularTotal_QuantidadeNegativa_LancaArgumentOutOfRangeException()
    {
        var produto = CriarProdutoValido();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => produto.CalcularTotal(-1));

        Assert.Equal("quantidade", erro.ParamName);
    }
}
