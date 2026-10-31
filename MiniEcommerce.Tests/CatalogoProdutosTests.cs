using MiniEcommerce.Models;
using MiniEcommerce.Services;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="CatalogoProdutos"/> (Etapa 1),
/// responsável pelo cadastro, consulta e controle de estoque dos produtos
/// (requisitos funcionais 1 e 4).
/// </summary>
public class CatalogoProdutosTests
{
    private static CatalogoProdutos CriarCatalogoVazio() => new();

    private static CatalogoProdutos CriarCatalogoPadrao()
    {
        var catalogo = new CatalogoProdutos();
        catalogo.Cadastrar("Teclado Mecânico", 249.90m, 10);
        catalogo.Cadastrar("Mouse Gamer", 129.90m, 5);
        catalogo.Cadastrar("Webcam Full HD", 199.00m, 0);
        return catalogo;
    }

    // ------------------------------------------------------------------
    // Cadastro
    // ------------------------------------------------------------------

    [Fact]
    public void Cadastrar_ProdutoValido_RetornaProdutoComIdSequencial()
    {
        var catalogo = CriarCatalogoVazio();

        var primeiro = catalogo.Cadastrar("Notebook", 3500m, 4);
        var segundo = catalogo.Cadastrar("Monitor", 900m, 7);

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
        Assert.Equal("Notebook", primeiro.Nome);
        Assert.Equal(3500m, primeiro.Preco);
        Assert.Equal(4, primeiro.QuantidadeEstoque);
        Assert.Equal(2, catalogo.TotalDeProdutos);
    }

    [Fact]
    public void Cadastrar_NomeDuplicado_LancaInvalidOperationException()
    {
        var catalogo = CriarCatalogoVazio();
        catalogo.Cadastrar("Mouse Gamer", 129.90m, 5);

        Assert.Throws<InvalidOperationException>(
            () => catalogo.Cadastrar("Mouse Gamer", 89.90m, 3));
    }

    [Fact]
    public void Cadastrar_NomeDuplicadoComCaixaDiferente_LancaInvalidOperationException()
    {
        var catalogo = CriarCatalogoVazio();
        catalogo.Cadastrar("Mouse Gamer", 129.90m, 5);

        Assert.Throws<InvalidOperationException>(
            () => catalogo.Cadastrar("  MOUSE GAMER  ", 89.90m, 3));
    }

    [Fact]
    public void Cadastrar_PrecoNegativo_LancaArgumentOutOfRangeException()
    {
        var catalogo = CriarCatalogoVazio();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => catalogo.Cadastrar("Headset", -50m, 3));

        Assert.Equal("preco", erro.ParamName);
    }

    [Fact]
    public void Cadastrar_EstoqueNegativo_LancaArgumentOutOfRangeException()
    {
        var catalogo = CriarCatalogoVazio();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(
            () => catalogo.Cadastrar("Headset", 150m, -3));

        Assert.Equal("quantidadeEstoque", erro.ParamName);
    }

    [Fact]
    public void Cadastrar_NomeEmBranco_LancaArgumentException()
    {
        var catalogo = CriarCatalogoVazio();

        var erro = Assert.Throws<ArgumentException>(() => catalogo.Cadastrar("   ", 150m, 3));

        Assert.Equal("nome", erro.ParamName);
    }

    [Fact]
    public void TryCadastrar_DadosValidos_RetornaTrueEProduto()
    {
        var catalogo = CriarCatalogoVazio();

        var sucesso = catalogo.TryCadastrar("SSD 1TB", 420m, 12, out var produto, out var erro);

        Assert.True(sucesso);
        Assert.NotNull(produto);
        Assert.Null(erro);
        Assert.Equal("SSD 1TB", produto!.Nome);
    }

    [Fact]
    public void TryCadastrar_PrecoNegativo_RetornaFalseComMensagemDeErro()
    {
        var catalogo = CriarCatalogoVazio();

        var sucesso = catalogo.TryCadastrar("SSD 1TB", -1m, 12, out var produto, out var erro);

        Assert.False(sucesso);
        Assert.Null(produto);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Equal(0, catalogo.TotalDeProdutos);
    }

    [Fact]
    public void TryCadastrar_NomeDuplicado_RetornaFalseComMensagemDeErro()
    {
        var catalogo = CriarCatalogoVazio();
        catalogo.Cadastrar("SSD 1TB", 420m, 12);

        var sucesso = catalogo.TryCadastrar("SSD 1TB", 380m, 9, out _, out var erro);

        Assert.False(sucesso);
        Assert.Contains("já existe", erro, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    // Consulta
    // ------------------------------------------------------------------

    [Fact]
    public void BuscarPorId_ProdutoExistente_RetornaProduto()
    {
        var catalogo = CriarCatalogoPadrao();

        var encontrado = catalogo.BuscarPorId(2);

        Assert.NotNull(encontrado);
        Assert.Equal("Mouse Gamer", encontrado!.Nome);
    }

    [Fact]
    public void BuscarPorId_ProdutoInexistente_RetornaNull()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.Null(catalogo.BuscarPorId(99));
    }

    [Fact]
    public void BuscarPorNome_IgnoraCaixaERetornaProduto()
    {
        var catalogo = CriarCatalogoPadrao();

        var encontrado = catalogo.BuscarPorNome("teclado mecânico");

        Assert.NotNull(encontrado);
        Assert.Equal(1, encontrado!.Id);
    }

    [Fact]
    public void BuscarPorNome_ProdutoInexistente_RetornaNull()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.Null(catalogo.BuscarPorNome("Impressora"));
    }

    [Fact]
    public void Existe_NomeCadastrado_RetornaTrue()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.True(catalogo.Existe("MOUSE GAMER"));
        Assert.False(catalogo.Existe("Impressora"));
    }

    [Fact]
    public void ListarDisponiveis_RetornaSomenteProdutosComEstoque()
    {
        var catalogo = CriarCatalogoPadrao();

        var disponiveis = catalogo.ListarDisponiveis().ToList();

        Assert.Equal(2, disponiveis.Count);
        Assert.DoesNotContain(disponiveis, p => p.Nome == "Webcam Full HD");
    }

    [Fact]
    public void ListarSemEstoque_RetornaSomenteProdutosZerados()
    {
        var catalogo = CriarCatalogoPadrao();

        var esgotados = catalogo.ListarSemEstoque().ToList();

        Assert.Single(esgotados);
        Assert.Equal("Webcam Full HD", esgotados[0].Nome);
    }

    [Fact]
    public void ValorTotalEmEstoque_SomaPrecoMultiplicadoPorEstoque()
    {
        var catalogo = CriarCatalogoVazio();
        catalogo.Cadastrar("Produto A", 10m, 3);
        catalogo.Cadastrar("Produto B", 25.50m, 2);

        Assert.Equal(81.00m, catalogo.ValorTotalEmEstoque());
    }

    [Fact]
    public void ObterListagemFormatada_ContemNomePrecoEEstoque()
    {
        var catalogo = CriarCatalogoPadrao();

        var listagem = catalogo.ObterListagemFormatada();

        Assert.Contains("Teclado Mecânico", listagem);
        Assert.Contains("Mouse Gamer", listagem);
        Assert.Contains("R$", listagem);
        Assert.Contains("249,90", listagem);
    }

    [Fact]
    public void ObterListagemFormatada_CatalogoVazio_ExibeMensagemInformativa()
    {
        var catalogo = CriarCatalogoVazio();

        var listagem = catalogo.ObterListagemFormatada();

        Assert.Contains("Nenhum produto cadastrado", listagem);
    }

    // ------------------------------------------------------------------
    // Remoção e movimentação de estoque
    // ------------------------------------------------------------------

    [Fact]
    public void Remover_ProdutoExistente_RetornaTrueEDiminuiCatalogo()
    {
        var catalogo = CriarCatalogoPadrao();

        var removido = catalogo.Remover(1);

        Assert.True(removido);
        Assert.Equal(2, catalogo.TotalDeProdutos);
        Assert.Null(catalogo.BuscarPorId(1));
    }

    [Fact]
    public void Remover_ProdutoInexistente_RetornaFalse()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.False(catalogo.Remover(99));
    }

    [Fact]
    public void BaixarEstoque_QuantidadeDisponivel_DebitaDoProduto()
    {
        var catalogo = CriarCatalogoPadrao();

        var sucesso = catalogo.BaixarEstoque(1, 4);

        Assert.True(sucesso);
        Assert.Equal(6, catalogo.BuscarPorId(1)!.QuantidadeEstoque);
    }

    [Fact]
    public void BaixarEstoque_QuantidadeIndisponivel_RetornaFalseEMantemEstoque()
    {
        var catalogo = CriarCatalogoPadrao();

        var sucesso = catalogo.BaixarEstoque(2, 50);

        Assert.False(sucesso);
        Assert.Equal(5, catalogo.BuscarPorId(2)!.QuantidadeEstoque);
    }

    [Fact]
    public void BaixarEstoque_ProdutoInexistente_RetornaFalse()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.False(catalogo.BaixarEstoque(99, 1));
    }

    [Fact]
    public void ReporEstoque_ProdutoExistente_SomaQuantidade()
    {
        var catalogo = CriarCatalogoPadrao();

        var sucesso = catalogo.ReporEstoque(3, 15);

        Assert.True(sucesso);
        Assert.Equal(15, catalogo.BuscarPorId(3)!.QuantidadeEstoque);
    }

    [Fact]
    public void Produtos_ColecaoRetornada_NaoPermiteAlteracaoExterna()
    {
        var catalogo = CriarCatalogoPadrao();

        Assert.IsAssignableFrom<IReadOnlyList<Produto>>(catalogo.Produtos);
        Assert.Throws<NotSupportedException>(() => ((IList<Produto>)catalogo.Produtos).Add(
            new Produto(99, "Invasor", 1m, 1)));
    }
}
