namespace ClientePagamentoSystem.Models;

public class FormaPagamento
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;

    public override string ToString() => Nome;
}
