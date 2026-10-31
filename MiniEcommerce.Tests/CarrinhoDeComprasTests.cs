using MiniEcommerce.Models;
using MiniEcommerce.Services;

namespace MiniEcommerce.Tests;

/// <summary>
/// Testes unitários do <see cref="CarrinhoDeCompras"/> (Etapa 2).
/// Cobrem adição com sucesso, limites de estoque, cálculo de subtotal e remoção de itens
/// (requisito funcional 2).
/// </summary>
public class CarrinhoDeComprasTests
{
    private static Produto CriarProduto(int id = 1, string nome = "Mouse Gamer", decimal preco = 100m, int estoque = 10) =>
        new(id, nome, preco, estoque);

    private static (CarrinhoDeCompras Carrinho, CatalogoProdutos Catalogo) CriarCenario()
    {
        var catalogo = new CatalogoProdutos();
        catalogo.Cadastrar("Mouse Gamer", 100m, 10);
        catalogo.Cadastrar("Teclado Mecânico", 250m, 3);
        catalogo.Cadastrar("Webcam Full HD", 180m, 0);

        return (new CarrinhoDeCompras(), catalogo);
    }

    // ------------------------------------------------------------------
    // Estado inicial
    // ------------------------------------------------------------------

    [Fact]
    public void Construtor_CarrinhoNovo_NasceVazio()
    {
        var carrinho = new CarrinhoDeCompras();

        Assert.True(carrinho.EstaVazio);
        Assert.Empty(carrinho.Itens);
        Assert.Equal(0, carrinho.QuantidadeTotalDeItens);
        Assert.Equal(0m, carrinho.Subtotal);
    }

    // ------------------------------------------------------------------
    // Adição de itens
    // ------------------------------------------------------------------

    [Fact]
    public void Adicionar_ProdutoComEstoque_InsereItemNoCarrinho()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto();

        carrinho.Adicionar(produto, 2);

        Assert.False(carrinho.EstaVazio);
        Assert.Single(carrinho.Itens);
        Assert.Equal(2, carrinho.Itens[0].Quantidade);
        Assert.Equal(200m, carrinho.Subtotal);
    }

    [Fact]
    public void Adicionar_SemInformarQuantidade_InsereUmaUnidade()
    {
        var carrinho = new CarrinhoDeCompras();

        carrinho.Adicionar(CriarProduto());

        Assert.Equal(1, carrinho.Itens[0].Quantidade);
        Assert.Equal(1, carrinho.QuantidadeTotalDeItens);
    }

    [Fact]
    public void Adicionar_ProdutoNulo_LancaArgumentNullException()
    {
        var carrinho = new CarrinhoDeCompras();

        var erro = Assert.Throws<ArgumentNullException>(() => carrinho.Adicionar(null!, 1));

        Assert.Equal("produto", erro.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Adicionar_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var carrinho = new CarrinhoDeCompras();

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => carrinho.Adicionar(CriarProduto(), quantidade));

        Assert.Equal("quantidade", erro.ParamName);
        Assert.True(carrinho.EstaVazio);
    }

    [Fact]
    public void Adicionar_QuantidadeIgualAoEstoque_PermiteInclusao()
    {
        var carrinho = new CarrinhoDeCompras();

        carrinho.Adicionar(CriarProduto(estoque: 5), 5);

        Assert.Equal(5, carrinho.Itens[0].Quantidade);
    }

    [Fact]
    public void Adicionar_QuantidadeSuperiorAoEstoque_LancaInvalidOperationException()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto(estoque: 4);

        var erro = Assert.Throws<InvalidOperationException>(() => carrinho.Adicionar(produto, 5));

        Assert.Contains("estoque insuficiente", erro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(carrinho.EstaVazio);
    }

    [Fact]
    public void Adicionar_ProdutoSemEstoque_LancaInvalidOperationException()
    {
        var carrinho = new CarrinhoDeCompras();

        Assert.Throws<InvalidOperationException>(() => carrinho.Adicionar(CriarProduto(estoque: 0), 1));
    }

    [Fact]
    public void Adicionar_MesmoProdutoDuasVezes_AcumulaNoMesmoItem()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto(estoque: 10);

        carrinho.Adicionar(produto, 2);
        carrinho.Adicionar(produto, 3);

        Assert.Single(carrinho.Itens);
        Assert.Equal(5, carrinho.Itens[0].Quantidade);
        Assert.Equal(500m, carrinho.Subtotal);
    }

    [Fact]
    public void Adicionar_AcumuloSuperiorAoEstoque_LancaExceptionEMantemQuantidadeAnterior()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto(estoque: 5);
        carrinho.Adicionar(produto, 3);

        Assert.Throws<InvalidOperationException>(() => carrinho.Adicionar(produto, 3));

        Assert.Equal(3, carrinho.Itens[0].Quantidade);
        Assert.Equal(300m, carrinho.Subtotal);
    }

    [Fact]
    public void Adicionar_ProdutosDiferentes_CriaItensSeparados()
    {
        var (carrinho, catalogo) = CriarCenario();

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 1);
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 2);

        Assert.Equal(2, carrinho.Itens.Count);
        Assert.Equal(3, carrinho.QuantidadeTotalDeItens);
        Assert.Equal(600m, carrinho.Subtotal);
    }

    [Fact]
    public void Adicionar_EstoqueRepostoDepois_PassaAPermitirQuantidadeMaior()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto(estoque: 2);
        carrinho.Adicionar(produto, 2);

        produto.ReporEstoque(5);
        carrinho.Adicionar(produto, 4);

        Assert.Equal(6, carrinho.Itens[0].Quantidade);
    }

    // ------------------------------------------------------------------
    // TryAdicionar
    // ------------------------------------------------------------------

    [Fact]
    public void TryAdicionar_DadosValidos_RetornaTrueSemErro()
    {
        var carrinho = new CarrinhoDeCompras();

        var sucesso = carrinho.TryAdicionar(CriarProduto(), 2, out var erro);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Single(carrinho.Itens);
    }

    [Fact]
    public void TryAdicionar_QuantidadeAcimaDoEstoque_RetornaFalseComMensagem()
    {
        var carrinho = new CarrinhoDeCompras();

        var sucesso = carrinho.TryAdicionar(CriarProduto(estoque: 2), 5, out var erro);

        Assert.False(sucesso);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Contains("estoque insuficiente", erro, StringComparison.OrdinalIgnoreCase);
        Assert.True(carrinho.EstaVazio);
    }

    [Fact]
    public void TryAdicionar_QuantidadeInvalida_RetornaFalseComMensagem()
    {
        var carrinho = new CarrinhoDeCompras();

        var sucesso = carrinho.TryAdicionar(CriarProduto(), 0, out var erro);

        Assert.False(sucesso);
        Assert.False(string.IsNullOrWhiteSpace(erro));
    }

    [Fact]
    public void TryAdicionar_ProdutoNulo_RetornaFalseSemLancarExcecao()
    {
        var carrinho = new CarrinhoDeCompras();

        var sucesso = carrinho.TryAdicionar(null!, 1, out var erro);

        Assert.False(sucesso);
        Assert.False(string.IsNullOrWhiteSpace(erro));
    }

    // ------------------------------------------------------------------
    // Consulta
    // ------------------------------------------------------------------

    [Fact]
    public void ObterItem_ProdutoNoCarrinho_RetornaItem()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 2);

        var item = carrinho.ObterItem(1);

        Assert.NotNull(item);
        Assert.Equal(2, item!.Quantidade);
    }

    [Fact]
    public void ObterItem_ProdutoForaDoCarrinho_RetornaNull()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 2);

        Assert.Null(carrinho.ObterItem(99));
    }

    [Fact]
    public void Contem_ProdutoNoCarrinho_RetornaTrue()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 1);

        Assert.True(carrinho.Contem(1));
        Assert.False(carrinho.Contem(42));
    }

    [Fact]
    public void Itens_ColecaoRetornada_NaoPermiteAlteracaoExterna()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 1);

        Assert.IsAssignableFrom<IReadOnlyList<ItemCarrinho>>(carrinho.Itens);
        Assert.Throws<NotSupportedException>(
            () => ((IList<ItemCarrinho>)carrinho.Itens).Add(new ItemCarrinho(CriarProduto(id: 2), 1)));
    }

    // ------------------------------------------------------------------
    // Subtotal
    // ------------------------------------------------------------------

    [Fact]
    public void Subtotal_SomaTodosOsItensDoCarrinho()
    {
        var (carrinho, catalogo) = CriarCenario();

        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2); // 2 x 100 = 200
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 3); // 3 x 250 = 750

        Assert.Equal(950m, carrinho.Subtotal);
    }

    [Fact]
    public void Subtotal_AposRemocao_RecalculaValor()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(preco: 30m), 2);

        carrinho.Remover(1);

        Assert.Equal(0m, carrinho.Subtotal);
    }

    // ------------------------------------------------------------------
    // Remoção
    // ------------------------------------------------------------------

    [Fact]
    public void Remover_ItemExistente_RetornaTrueERemoveDoCarrinho()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 2);

        var removido = carrinho.Remover(1);

        Assert.True(removido);
        Assert.True(carrinho.EstaVazio);
    }

    [Fact]
    public void Remover_ItemInexistente_RetornaFalse()
    {
        var carrinho = new CarrinhoDeCompras();

        Assert.False(carrinho.Remover(99));
    }

    [Fact]
    public void RemoverQuantidade_Parcial_DiminuiQuantidadeDoItem()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 5);

        var removido = carrinho.RemoverQuantidade(1, 2);

        Assert.True(removido);
        Assert.Single(carrinho.Itens);
        Assert.Equal(3, carrinho.Itens[0].Quantidade);
    }

    [Fact]
    public void RemoverQuantidade_TotalDoItem_RemoveItemDoCarrinho()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 5);

        var removido = carrinho.RemoverQuantidade(1, 5);

        Assert.True(removido);
        Assert.True(carrinho.EstaVazio);
    }

    [Fact]
    public void RemoverQuantidade_SuperiorAoItem_RetornaFalseEMantemQuantidade()
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 2);

        var removido = carrinho.RemoverQuantidade(1, 5);

        Assert.False(removido);
        Assert.Equal(2, carrinho.Itens[0].Quantidade);
    }

    [Fact]
    public void RemoverQuantidade_ItemInexistente_RetornaFalse()
    {
        var carrinho = new CarrinhoDeCompras();

        Assert.False(carrinho.RemoverQuantidade(99, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RemoverQuantidade_QuantidadeInvalida_LancaArgumentOutOfRangeException(int quantidade)
    {
        var carrinho = new CarrinhoDeCompras();
        carrinho.Adicionar(CriarProduto(), 2);

        var erro = Assert.Throws<ArgumentOutOfRangeException>(() => carrinho.RemoverQuantidade(1, quantidade));

        Assert.Equal("quantidade", erro.ParamName);
    }

    [Fact]
    public void Remover_ProdutoPermiteNovaAdicaoDentroDoEstoque()
    {
        var carrinho = new CarrinhoDeCompras();
        var produto = CriarProduto(estoque: 2);
        carrinho.Adicionar(produto, 2);

        carrinho.Remover(1);
        carrinho.Adicionar(produto, 2);

        Assert.Equal(2, carrinho.Itens[0].Quantidade);
    }

    [Fact]
    public void Limpar_CarrinhoComItens_EsvaziaCarrinho()
    {
        var (carrinho, catalogo) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2);
        carrinho.Adicionar(catalogo.BuscarPorId(2)!, 1);

        carrinho.Limpar();

        Assert.True(carrinho.EstaVazio);
        Assert.Empty(carrinho.Itens);
        Assert.Equal(0m, carrinho.Subtotal);
    }

    // ------------------------------------------------------------------
    // Resumo para o console
    // ------------------------------------------------------------------

    [Fact]
    public void ObterResumoFormatado_ComItens_ContemNomeQuantidadeETotal()
    {
        var (carrinho, catalogo) = CriarCenario();
        carrinho.Adicionar(catalogo.BuscarPorId(1)!, 2);

        var resumo = carrinho.ObterResumoFormatado();

        Assert.Contains("Mouse Gamer", resumo);
        Assert.Contains("R$", resumo);
        Assert.Contains("200", resumo);
    }

    [Fact]
    public void ObterResumoFormatado_CarrinhoVazio_ExibeMensagemInformativa()
    {
        var carrinho = new CarrinhoDeCompras();

        Assert.Contains("carrinho está vazio", carrinho.ObterResumoFormatado(), StringComparison.OrdinalIgnoreCase);
    }
}
