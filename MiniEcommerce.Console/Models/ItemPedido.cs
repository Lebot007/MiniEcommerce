using System.Globalization;

namespace MiniEcommerce.Models;

/// <summary>
/// Linha de um pedido já finalizado (Etapa 3).
/// Diferente do <see cref="ItemCarrinho"/>, o item do pedido é uma <b>fotografia</b> da compra:
/// congela o nome e o preço unitário praticados no momento do checkout, de modo que reajustes
/// posteriores no catálogo não alterem pedidos já emitidos.
/// </summary>
public class ItemPedido
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Identificador do produto adquirido.</summary>
    public int ProdutoId { get; }

    /// <summary>Nome do produto no momento da compra.</summary>
    public string NomeProduto { get; }

    /// <summary>Preço unitário praticado no momento da compra. Nunca negativo.</summary>
    public decimal PrecoUnitario { get; }

    /// <summary>Quantidade adquirida. Sempre maior que zero.</summary>
    public int Quantidade { get; }

    /// <summary>Valor da linha (preço unitário x quantidade).</summary>
    public decimal Subtotal => PrecoUnitario * Quantidade;

    /// <summary>Cria o item congelando os dados do produto no momento da compra.</summary>
    /// <exception cref="ArgumentNullException">Quando <paramref name="produto"/> é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public ItemPedido(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade do item deve ser maior que zero.");
        }

        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        PrecoUnitario = produto.Preco;
        Quantidade = quantidade;
    }

    /// <summary>Cria o item a partir dos valores brutos (útil para reconstruir um pedido).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="produtoId"/> é menor ou igual a zero,
    /// <paramref name="precoUnitario"/> é negativo ou <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    /// <exception cref="ArgumentException">Quando <paramref name="nomeProduto"/> é nulo ou em branco.</exception>
    public ItemPedido(int produtoId, string nomeProduto, decimal precoUnitario, int quantidade)
    {
        if (produtoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(produtoId), "O identificador do produto deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(nomeProduto))
        {
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(nomeProduto));
        }

        if (precoUnitario < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(precoUnitario), "O preço unitário não pode ser negativo.");
        }

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade do item deve ser maior que zero.");
        }

        ProdutoId = produtoId;
        NomeProduto = nomeProduto.Trim();
        PrecoUnitario = precoUnitario;
        Quantidade = quantidade;
    }

    public override string ToString() =>
        $"{NomeProduto,-28} {Quantidade,3} x {PrecoUnitario.ToString("C", CulturaBrasileira),12} = {Subtotal.ToString("C", CulturaBrasileira),12}";
}
