using System.Globalization;

namespace MiniEcommerce.Models;

/// <summary>
/// Representa uma linha do carrinho de compras: o produto escolhido e a quantidade solicitada.
/// </summary>
public class ItemCarrinho
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Produto adicionado ao carrinho.</summary>
    public Produto Produto { get; }

    /// <summary>Quantidade de unidades do produto no carrinho. Sempre maior que zero.</summary>
    public int Quantidade { get; private set; }

    /// <summary>Valor da linha (preço unitário x quantidade).</summary>
    public decimal Subtotal => Produto.CalcularTotal(Quantidade);

    /// <exception cref="ArgumentNullException">Quando <paramref name="produto"/> é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public ItemCarrinho(Produto produto, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(produto);

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade do item deve ser maior que zero.");
        }

        Produto = produto;
        Quantidade = quantidade;
    }

    /// <summary>Altera a quantidade do item no carrinho.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public void AjustarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade do item deve ser maior que zero.");
        }

        Quantidade = quantidade;
    }

    public override string ToString() =>
        $"{Produto.Nome,-28} {Quantidade,3} x {Produto.Preco.ToString("C", CulturaBrasileira),12} = {Subtotal.ToString("C", CulturaBrasileira),12}";
}
