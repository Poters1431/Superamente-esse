using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClientePagamentoSystem.Services;

/// <summary>
/// Consulta de CEP na API pública ViaCEP (https://viacep.com.br).
/// Retorna logradouro/bairro/cidade/UF ou null se o CEP não existir.
/// </summary>
public class CepService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public record EnderecoCep(
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] bool? Erro);

    public async Task<EnderecoCep?> BuscarAsync(string cep)
    {
        var numeros = new string(cep.Where(char.IsDigit).ToArray());
        if (numeros.Length != 8)
            throw new ArgumentException("CEP inválido. Digite os 8 números do CEP.");

        var json = await _http.GetStringAsync($"https://viacep.com.br/ws/{numeros}/json/");
        var endereco = JsonSerializer.Deserialize<EnderecoCep>(json);

        if (endereco is null || endereco.Erro == true)
            return null;

        return endereco;
    }
}
