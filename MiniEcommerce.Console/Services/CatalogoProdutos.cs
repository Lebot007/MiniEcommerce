using System.Text;
using MiniEcommerce.Infrastructure;
using MiniEcommerce.Models;

namespace MiniEcommerce.Services;

/// <summary>
/// Catálogo da loja: responsável pelo cadastro, consulta e movimentação de
/// estoque dos produtos (requisitos funcionais 1 e 4).
/// </summary>
public class CatalogoProdutos
{
    private readonly List<Produto> _produtos = new();
    private int _ultimoIdGerado;

    /// <summary>Lista somente-leitura com todos os produtos cadastrados.</summary>
    public IReadOnlyList<Produto> Produtos => _produtos.AsReadOnly();

    /// <summary>Quantidade de produtos cadastrados no catálogo.</summary>
    public int TotalDeProdutos => _produtos.Count;

    /// <summary>
    /// Cadastra um novo produto atribuindo automaticamente o próximo identificador.
    /// </summary>
    /// <exception cref="ArgumentException">Quando o nome é nulo ou em branco.</exception>
    /// <exception cref="InvalidOperationException">Quando já existe produto com o mesmo nome.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quando o preço ou o estoque são negativos.</exception>
    public Produto Cadastrar(string nome, decimal preco, int quantidadeEstoque)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(nome));
        }

        var nomeNormalizado = nome.Trim();

        if (Existe(nomeNormalizado))
        {
            throw new InvalidOperationException($"Já existe um produto cadastrado com o nome '{nomeNormalizado}'.");
        }

        var produto = new Produto(_ultimoIdGerado + 1, nomeNormalizado, preco, quantidadeEstoque);

        _produtos.Add(produto);
        _ultimoIdGerado = produto.Id;

        return produto;
    }

    /// <summary>
    /// Versão segura de <see cref="Cadastrar"/>: não lança exceções de regra de negócio,
    /// devolvendo <c>false</c> e a mensagem do problema em <paramref name="erro"/>.
    /// </summary>
    public bool TryCadastrar(string nome, decimal preco, int quantidadeEstoque, out Produto? produto, out string? erro)
    {
        try
        {
            produto = Cadastrar(nome, preco, quantidadeEstoque);
            erro = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            produto = null;
            erro = ex.Message;
            return false;
        }
    }

    /// <summary>Localiza um produto pelo identificador. Retorna <c>null</c> se não existir.</summary>
    public Produto? BuscarPorId(int id) => _produtos.Find(p => p.Id == id);

    /// <summary>Localiza um produto pelo nome, ignorando maiúsculas/minúsculas.</summary>
    public Produto? BuscarPorNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return null;
        }

        var nomeNormalizado = nome.Trim();

        return _produtos.Find(p => string.Equals(p.Nome, nomeNormalizado, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Indica se já existe produto cadastrado com o nome informado.</summary>
    public bool Existe(string nome) => BuscarPorNome(nome) is not null;

    /// <summary>Remove um produto do catálogo. Retorna <c>false</c> se ele não existir.</summary>
    public bool Remover(int id)
    {
        var produto = BuscarPorId(id);

        if (produto is null)
        {
            return false;
        }

        return _produtos.Remove(produto);
    }

    /// <summary>Produtos com estoque disponível para venda.</summary>
    public IEnumerable<Produto> ListarDisponiveis() => _produtos.Where(p => p.EstaDisponivel);

    /// <summary>Produtos com estoque zerado.</summary>
    public IEnumerable<Produto> ListarSemEstoque() => _produtos.Where(p => !p.EstaDisponivel);

    /// <summary>Soma do valor de todos os itens parados em estoque.</summary>
    public decimal ValorTotalEmEstoque() => _produtos.Sum(p => p.CalcularTotal(p.QuantidadeEstoque));

    /// <summary>
    /// Dá baixa no estoque de um produto. Retorna <c>false</c> quando o produto não existe
    /// ou quando não há unidades suficientes.
    /// </summary>
    public bool BaixarEstoque(int produtoId, int quantidade)
    {
        var produto = BuscarPorId(produtoId);

        return produto is not null && produto.RetirarDoEstoque(quantidade);
    }

    /// <summary>Repõe unidades no estoque de um produto cadastrado.</summary>
    public bool ReporEstoque(int produtoId, int quantidade)
    {
        var produto = BuscarPorId(produtoId);

        if (produto is null)
        {
            return false;
        }

        produto.ReporEstoque(quantidade);
        return true;
    }

    /// <summary>Monta a listagem do catálogo (id, nome, preço e estoque) para exibição no console.</summary>
    public string ObterListagemFormatada()
    {
        if (_produtos.Count == 0)
        {
            return "Nenhum produto cadastrado no catálogo.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{"ID",-4}{"Produto",-30}{"Preço",14}{"Estoque",10}");
        sb.AppendLine(new string('-', 58));

        foreach (var produto in _produtos)
        {
            var preco = produto.Preco.Moeda();
            var situacao = produto.EstaDisponivel ? string.Empty : "  (esgotado)";

            sb.AppendLine($"{produto.Id,-4}{produto.Nome,-30}{preco,14}{produto.QuantidadeEstoque,10}{situacao}");
        }

        sb.AppendLine(new string('-', 58));
        sb.AppendLine($"Total de produtos: {_produtos.Count} | Valor em estoque: {ValorTotalEmEstoque().Moeda()}");

        return sb.ToString();
    }
}
