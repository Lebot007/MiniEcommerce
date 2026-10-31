namespace MiniEcommerce.Models;

/// <summary>
/// Entidade que representa um produto vendido pela loja.
/// Concentra as regras de validação (preço não negativo e estoque nunca menor
/// que zero) e as movimentações de estoque usadas no checkout.
/// </summary>
public class Produto
{
    /// <summary>Identificador único do produto dentro do catálogo.</summary>
    public int Id { get; }

    /// <summary>Nome do produto (obrigatório e sem espaços nas extremidades).</summary>
    public string Nome { get; }

    /// <summary>Preço unitário. Não pode ser negativo.</summary>
    public decimal Preco { get; private set; }

    /// <summary>Quantidade disponível em estoque. Nunca menor que zero.</summary>
    public int QuantidadeEstoque { get; private set; }

    /// <summary>Indica se o produto está disponível para venda (estoque &gt; 0).</summary>
    public bool EstaDisponivel => QuantidadeEstoque > 0;

    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="id"/> é menor ou igual a zero,
    /// <paramref name="preco"/> é negativo ou <paramref name="quantidadeEstoque"/> é negativo.</exception>
    /// <exception cref="ArgumentException">Quando <paramref name="nome"/> é nulo ou em branco.</exception>
    public Produto(int id, string nome, decimal preco, int quantidadeEstoque)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "O identificador do produto deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(nome));
        }

        if (preco < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(preco), "O preço do produto não pode ser negativo.");
        }

        if (quantidadeEstoque < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidadeEstoque), "A quantidade em estoque não pode ser menor que zero.");
        }

        Id = id;
        Nome = nome.Trim();
        Preco = preco;
        QuantidadeEstoque = quantidadeEstoque;
    }

    /// <summary>Verifica se há estoque suficiente para atender à quantidade solicitada.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public bool TemEstoqueSuficiente(int quantidade)
    {
        ValidarQuantidade(quantidade);

        return QuantidadeEstoque >= quantidade;
    }

    /// <summary>
    /// Debita a quantidade informada do estoque. Retorna <c>false</c> (sem efeito colateral)
    /// quando não há estoque suficiente.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public bool RetirarDoEstoque(int quantidade)
    {
        if (!TemEstoqueSuficiente(quantidade))
        {
            return false;
        }

        QuantidadeEstoque -= quantidade;
        return true;
    }

    /// <summary>Acrescenta unidades ao estoque (entrada de mercadoria).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é menor ou igual a zero.</exception>
    public void ReporEstoque(int quantidade)
    {
        ValidarQuantidade(quantidade);

        QuantidadeEstoque += quantidade;
    }

    /// <summary>Atualiza o preço unitário do produto.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="novoPreco"/> é negativo.</exception>
    public void AtualizarPreco(decimal novoPreco)
    {
        if (novoPreco < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(novoPreco), "O preço do produto não pode ser negativo.");
        }

        Preco = novoPreco;
    }

    /// <summary>Calcula o valor total para a quantidade informada.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="quantidade"/> é negativa.</exception>
    public decimal CalcularTotal(int quantidade)
    {
        if (quantidade < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade não pode ser negativa.");
        }

        return Preco * quantidade;
    }

    public override string ToString() => $"{Nome} (R$ {Preco:N2}) - estoque: {QuantidadeEstoque}";

    private static void ValidarQuantidade(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade deve ser maior que zero.");
        }
    }
}
