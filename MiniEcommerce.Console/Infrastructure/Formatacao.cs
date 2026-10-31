using System.Globalization;

namespace MiniEcommerce.Infrastructure;

/// <summary>
/// Ponto único de formatação e conversão de valores da aplicação, no padrão brasileiro.
/// Centraliza a cultura <c>pt-BR</c> que antes era declarada de forma duplicada em cada
/// classe do domínio, garantindo saída consistente (<c>R$ 1.234,56</c> e <c>dd/MM/aaaa</c>)
/// independentemente da cultura do sistema operacional.
/// </summary>
public static class Formatacao
{
    /// <summary>Cultura brasileira compartilhada por entidades, serviços e console.</summary>
    public static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Formata um valor como moeda brasileira (ex.: <c>R$ 1.234,56</c>).</summary>
    public static string Moeda(this decimal valor) => valor.ToString("C", CulturaBrasileira);

    /// <summary>Formata data e hora no padrão brasileiro curto (ex.: <c>31/10/2026 15:04</c>).</summary>
    public static string DataHora(this DateTime valor) => valor.ToString("dd/MM/yyyy HH:mm", CulturaBrasileira);

    /// <summary>
    /// Converte texto digitado pelo usuário em <see cref="decimal"/>, aceitando os dois
    /// formatos mais comuns: vírgula decimal brasileira (ex.: <c>1.234,56</c>) ou ponto
    /// decimal internacional (ex.: <c>249.90</c>).
    /// </summary>
    public static bool TentarConverterDecimal(string? texto, out decimal valor)
    {
        valor = 0m;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpo = texto.Trim();
        var cultura = limpo.Contains(',') ? CulturaBrasileira : CultureInfo.InvariantCulture;

        return decimal.TryParse(limpo, NumberStyles.Number, cultura, out valor);
    }

    /// <summary>Converte texto digitado pelo usuário em número inteiro.</summary>
    public static bool TentarConverterInteiro(string? texto, out int valor)
    {
        valor = 0;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return int.TryParse(texto.Trim(), NumberStyles.Integer, CulturaBrasileira, out valor);
    }
}
