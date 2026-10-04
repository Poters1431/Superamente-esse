namespace ClientePagamentoSystem.Models;

public class Cliente
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Endereco { get; set; }
    public bool Ativo { get; set; } = true;

    public override string ToString() => NomeCompleto;
}
