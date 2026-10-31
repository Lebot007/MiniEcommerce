using System.Globalization;
using System.Text;

namespace MiniEcommerce.Models;

/// <summary>
/// Pedido finalizado (requisito funcional 3). É imutável depois de criado: guarda os itens
/// comprados, a data do fechamento e o valor total calculado a partir dos itens selecionados,
/// além de gerar o resumo financeiro exibido ao cliente.
/// </summary>
public class Pedido
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    private readonly List<ItemPedido> _itens;

    /// <summary>Número sequencial do pedido. Sempre maior que zero.</summary>
    public int Numero { get; }

    /// <summary>Data e hora em que o pedido foi finalizado.</summary>
    public DateTime RealizadoEm { get; }

    /// <summary>Itens adquiridos (somente leitura).</summary>
    public IReadOnlyList<ItemPedido> Itens => _itens.AsReadOnly();

    /// <summary>Quantidade de produtos distintos no pedido.</summary>
    public int TotalDeProdutos => _itens.Count;

    /// <summary>Soma das unidades adquiridas em todos os itens.</summary>
    public int QuantidadeTotalDeItens => _itens.Sum(i => i.Quantidade);

    /// <summary>Valor total do pedido (soma dos subtotais), calculado uma única vez na criação.</summary>
    public decimal ValorTotal { get; }

    /// <summary>Valor total já formatado em moeda brasileira.</summary>
    public string ValorTotalFormatado => ValorTotal.ToString("C", CulturaBrasileira);

    /// <param name="numero">Número sequencial do pedido (maior que zero).</param>
    /// <param name="itens">Itens adquiridos; ao menos um, nenhum nulo.</param>
    /// <param name="realizadoEm">Data do fechamento. Quando omitida, usa o momento atual.</param>
    /// <exception cref="ArgumentNullException">Quando <paramref name="itens"/> é nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quando <paramref name="numero"/> é menor ou igual a zero.</exception>
    /// <exception cref="ArgumentException">Quando <paramref name="itens"/> está vazio ou contém elemento nulo.</exception>
    public Pedido(int numero, IEnumerable<ItemPedido> itens, DateTime? realizadoEm = null)
    {
        ArgumentNullException.ThrowIfNull(itens);

        if (numero <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numero), "O número do pedido deve ser maior que zero.");
        }

        var listaItens = itens.ToList();

        if (listaItens.Count == 0)
        {
            throw new ArgumentException("O pedido deve conter ao menos um item.", nameof(itens));
        }

        if (listaItens.Exists(i => i is null))
        {
            throw new ArgumentException("O pedido não pode conter itens nulos.", nameof(itens));
        }

        Numero = numero;
        _itens = listaItens;
        RealizadoEm = realizadoEm ?? DateTime.Now;
        ValorTotal = listaItens.Sum(i => i.Subtotal);
    }

    /// <summary>Gera o resumo financeiro do pedido (itens, quantidades, valores e total).</summary>
    public string ObterResumoFormatado()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Pedido nº {Numero} - realizado em {RealizadoEm:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"{"Produto",-30}{"Qtd.",6}{"Preço unit.",16}{"Subtotal",16}");
        sb.AppendLine(new string('-', 68));

        foreach (var item in _itens)
        {
            sb.AppendLine(
                $"{item.NomeProduto,-30}{item.Quantidade,6}" +
                $"{item.PrecoUnitario.ToString("C", CulturaBrasileira),16}" +
                $"{item.Subtotal.ToString("C", CulturaBrasileira),16}");
        }

        sb.AppendLine(new string('-', 68));
        sb.AppendLine($"Produtos distintos: {TotalDeProdutos} | Unidades: {QuantidadeTotalDeItens} | " +
                      $"Valor total: {ValorTotalFormatado}");

        return sb.ToString();
    }

    public override string ToString() =>
        $"Pedido nº {Numero} ({RealizadoEm:dd/MM/yyyy HH:mm}) - {TotalDeProdutos} produto(s), " +
        $"{QuantidadeTotalDeItens} unidade(s) - {ValorTotalFormatado}";
}
