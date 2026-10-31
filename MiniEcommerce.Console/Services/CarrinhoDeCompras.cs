using System.Globalization;
using System.Text;
using MiniEcommerce.Models;

namespace MiniEcommerce.Services;

/// <summary>
/// Carrinho de compras da loja (requisito funcional 2): adiciona produtos informando a
/// quantidade desejada, valida o limite de estoque disponível, permite remover itens e
/// calcula o subtotal da compra.
/// </summary>
public class CarrinhoDeCompras
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    private readonly List<ItemCarrinho> _itens = new();

    /// <summary>Itens atualmente no carrinho (somente leitura).</summary>
    public IReadOnlyList<ItemCarrinho> Itens => _itens.AsReadOnly();

    /// <summary>Soma das unidades de todos os itens do carrinho.</summary>
    public int QuantidadeTotalDeItens => _itens.Sum(i => i.Quantidade);

    /// <summary>Quantidade de produtos distintos no carrinho.</summary>
    public int TotalDeProdutos => _itens.Count;

    /// <summary>Valor total dos itens selecionados.</summary>
    public decimal Subtotal => _itens.Sum(i => i.Subtotal);

    /// <summary>Indica se o carrinho não possui itens.</summary>
    public bool EstaVazio => _itens.Count == 0;

    /// <summary>
    /// Adiciona um produto ao carrinho. Se o produto já estiver no carrinho, a quantidade é acumulada.
    /// </summary>
    /// <exception cref="ArgumentNullException">Quando <paramref name="produto"/> é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    /// <exception cref="InvalidOperationException">Quando a quantidade solicitada (somada à que já está
    /// no carrinho) ultrapassa o estoque disponível do produto.</exception>
    public void Adicionar(Produto produto, int quantidade = 1)
    {
        ArgumentNullException.ThrowIfNull(produto);

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade a adicionar deve ser maior que zero.");
        }

        var itemExistente = ObterItem(produto.Id);
        var quantidadeDesejada = (itemExistente?.Quantidade ?? 0) + quantidade;

        if (!produto.TemEstoqueSuficiente(quantidadeDesejada))
        {
            throw new InvalidOperationException(
                $"Estoque insuficiente para o produto '{produto.Nome}': disponível {produto.QuantidadeEstoque}, " +
                $"solicitado {quantidadeDesejada}.");
        }

        if (itemExistente is null)
        {
            _itens.Add(new ItemCarrinho(produto, quantidade));
            return;
        }

        itemExistente.AjustarQuantidade(quantidadeDesejada);
    }

    /// <summary>
    /// Versão segura de <see cref="Adicionar"/>: devolve <c>false</c> e a mensagem do problema
    /// em <paramref name="erro"/> em vez de lançar exceção.
    /// </summary>
    public bool TryAdicionar(Produto produto, int quantidade, out string? erro)
    {
        try
        {
            Adicionar(produto, quantidade);
            erro = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            erro = ex.Message;
            return false;
        }
    }

    /// <summary>Obtém o item do carrinho referente ao produto informado. Retorna <c>null</c> se não houver.</summary>
    public ItemCarrinho? ObterItem(int produtoId) => _itens.Find(i => i.Produto.Id == produtoId);

    /// <summary>Indica se o produto está no carrinho.</summary>
    public bool Contem(int produtoId) => ObterItem(produtoId) is not null;

    /// <summary>Remove totalmente um produto do carrinho.</summary>
    public bool Remover(int produtoId)
    {
        var item = ObterItem(produtoId);

        if (item is null)
        {
            return false;
        }

        return _itens.Remove(item);
    }

    /// <summary>
    /// Remove parte da quantidade de um item. Retorna <c>false</c> quando o produto não está no
    /// carrinho ou quando a quantidade informada é maior do que a existente no item.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public bool RemoverQuantidade(int produtoId, int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade a remover deve ser maior que zero.");
        }

        var item = ObterItem(produtoId);

        if (item is null || quantidade > item.Quantidade)
        {
            return false;
        }

        if (quantidade == item.Quantidade)
        {
            return Remover(produtoId);
        }

        item.AjustarQuantidade(item.Quantidade - quantidade);
        return true;
    }

    /// <summary>Esvazia o carrinho.</summary>
    public void Limpar() => _itens.Clear();

    /// <summary>Monta o resumo do carrinho (itens, quantidades e total) para exibição no console.</summary>
    public string ObterResumoFormatado()
    {
        if (EstaVazio)
        {
            return "O carrinho está vazio.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{"Produto",-30}{"Qtd.",6}{"Preço unit.",16}{"Subtotal",16}");
        sb.AppendLine(new string('-', 68));

        foreach (var item in _itens)
        {
            sb.AppendLine(
                $"{item.Produto.Nome,-30}{item.Quantidade,6}" +
                $"{item.Produto.Preco.ToString("C", CulturaBrasileira),16}" +
                $"{item.Subtotal.ToString("C", CulturaBrasileira),16}");
        }

        sb.AppendLine(new string('-', 68));
        sb.AppendLine($"Produtos distintos: {TotalDeProdutos} | Unidades: {QuantidadeTotalDeItens} | " +
                      $"Subtotal: {Subtotal.ToString("C", CulturaBrasileira)}");

        return sb.ToString();
    }
}
