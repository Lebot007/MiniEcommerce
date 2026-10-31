using System.Text;
using MiniEcommerce.Infrastructure;
using MiniEcommerce.Models;

namespace MiniEcommerce.Services;

/// <summary>
/// Processa o fechamento de pedidos (requisito funcional 3): valida o carrinho, dá baixa
/// automática no estoque dos produtos adquiridos, calcula o valor total da compra e mantém
/// o histórico de pedidos da loja.
/// </summary>
/// <remarks>
/// O checkout é <b>atômico</b>: todos os itens são validados antes de qualquer efeito colateral.
/// Se um único item estiver irregular (produto fora do catálogo ou estoque insuficiente), nenhum
/// estoque é debitado, nenhum pedido é gerado e o carrinho permanece intacto para correção.
/// </remarks>
public class ProcessadorDePedidos
{
    private readonly CatalogoProdutos _catalogo;
    private readonly List<Pedido> _pedidos = new();
    private int _ultimoNumeroGerado;

    /// <exception cref="ArgumentNullException">Quando <paramref name="catalogo"/> é nulo.</exception>
    public ProcessadorDePedidos(CatalogoProdutos catalogo)
    {
        ArgumentNullException.ThrowIfNull(catalogo);

        _catalogo = catalogo;
    }

    /// <summary>Pedidos já finalizados, em ordem de emissão (somente leitura).</summary>
    public IReadOnlyList<Pedido> Pedidos => _pedidos.AsReadOnly();

    /// <summary>Quantidade de pedidos finalizados.</summary>
    public int TotalDePedidos => _pedidos.Count;

    /// <summary>Número que será atribuído ao próximo pedido.</summary>
    public int ProximoNumero => _ultimoNumeroGerado + 1;

    /// <summary>Soma das unidades vendidas em todos os pedidos.</summary>
    public int QuantidadeTotalDeItensVendidos => _pedidos.Sum(p => p.QuantidadeTotalDeItens);

    /// <summary>Soma do valor total de todos os pedidos finalizados.</summary>
    public decimal FaturamentoTotal => _pedidos.Sum(p => p.ValorTotal);

    /// <summary>
    /// Confirma a compra: valida o carrinho, debita o estoque dos produtos adquiridos,
    /// emite o pedido numerado sequencialmente e esvazia o carrinho.
    /// </summary>
    /// <exception cref="ArgumentNullException">Quando <paramref name="carrinho"/> é nulo.</exception>
    /// <exception cref="InvalidOperationException">Quando o carrinho está vazio, quando algum item não
    /// consta no catálogo ou quando não há estoque suficiente para atendê-lo.</exception>
    public Pedido Finalizar(CarrinhoDeCompras carrinho)
    {
        ArgumentNullException.ThrowIfNull(carrinho);

        if (carrinho.EstaVazio)
        {
            throw new InvalidOperationException("Não é possível finalizar o pedido: o carrinho está vazio.");
        }

        // Fase 1 — valida tudo ANTES de qualquer baixa, garantindo o comportamento tudo-ou-nada.
        ValidarItensDoCarrinho(carrinho);

        // Fase 2 — com todos os itens validados, a baixa de estoque não tem como falhar.
        var itens = new List<ItemPedido>(carrinho.Itens.Count);

        foreach (var item in carrinho.Itens)
        {
            if (!_catalogo.BaixarEstoque(item.Produto.Id, item.Quantidade))
            {
                throw new InvalidOperationException(
                    $"Falha ao dar baixa no estoque do produto '{item.Produto.Nome}' (código {item.Produto.Id}).");
            }

            itens.Add(new ItemPedido(item.Produto, item.Quantidade));
        }

        var pedido = new Pedido(ProximoNumero, itens);

        _pedidos.Add(pedido);
        _ultimoNumeroGerado = pedido.Numero;

        carrinho.Limpar();

        return pedido;
    }

    /// <summary>
    /// Versão segura de <see cref="Finalizar"/>: não lança exceções de regra de negócio, devolvendo
    /// <c>false</c> e a mensagem do problema em <paramref name="erro"/>.
    /// </summary>
    public bool TryFinalizar(CarrinhoDeCompras carrinho, out Pedido? pedido, out string? erro)
    {
        try
        {
            pedido = Finalizar(carrinho);
            erro = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            pedido = null;
            erro = ex.Message;
            return false;
        }
    }

    /// <summary>Localiza um pedido pelo número. Retorna <c>null</c> se não existir.</summary>
    public Pedido? BuscarPorNumero(int numero) => _pedidos.Find(p => p.Numero == numero);

    /// <summary>Monta o histórico de pedidos com o faturamento acumulado para exibição no console.</summary>
    public string ObterHistoricoFormatado()
    {
        if (_pedidos.Count == 0)
        {
            return "Nenhum pedido finalizado até o momento.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{"Nº",6}{"Realizado em",20}{"Produtos",10}{"Unidades",10}{"Valor total",18}");
        sb.AppendLine(new string('-', 64));

        foreach (var pedido in _pedidos)
        {
            sb.AppendLine(
                $"{pedido.Numero,6}{pedido.RealizadoEm.DataHora(),20}" +
                $"{pedido.TotalDeProdutos,10}{pedido.QuantidadeTotalDeItens,10}" +
                $"{pedido.ValorTotal.Moeda(),18}");
        }

        sb.AppendLine(new string('-', 64));
        sb.AppendLine($"Pedidos: {TotalDePedidos} | Unidades vendidas: {QuantidadeTotalDeItensVendidos} | " +
                      $"Faturamento: {FaturamentoTotal.Moeda()}");

        return sb.ToString();
    }

    /// <summary>
    /// Verifica, sem aplicar efeito colateral, se todos os itens do carrinho podem ser vendidos:
    /// o produto precisa estar cadastrado no catálogo e ter estoque suficiente.
    /// </summary>
    private void ValidarItensDoCarrinho(CarrinhoDeCompras carrinho)
    {
        var problemas = new List<string>();

        foreach (var item in carrinho.Itens)
        {
            var produto = _catalogo.BuscarPorId(item.Produto.Id);

            if (produto is null)
            {
                problemas.Add($"o produto '{item.Produto.Nome}' (código {item.Produto.Id}) não consta no catálogo");
                continue;
            }

            if (!produto.TemEstoqueSuficiente(item.Quantidade))
            {
                problemas.Add(
                    $"estoque insuficiente para '{produto.Nome}': disponível {produto.QuantidadeEstoque}, " +
                    $"solicitado {item.Quantidade}");
            }
        }

        if (problemas.Count > 0)
        {
            throw new InvalidOperationException(
                "Não foi possível finalizar o pedido: " + string.Join("; ", problemas) + ".");
        }
    }
}
