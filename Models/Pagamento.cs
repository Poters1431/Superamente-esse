namespace ClientePagamentoSystem.Models;

public class Pagamento
{
    /// <summary>Prefixo usado na observação para marcar uma baixa rápida (Descontar).</summary>
    public const string BaixaRapidaPrefixo = "[BAIXA]";

    public int Id { get; set; }
    public int ClienteId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public int FormaPagamentoId { get; set; }
    public string Status { get; set; } = "Pendente"; // Pendente | Pago | Atrasado
    public string? Observacao { get; set; }
    public int UsuarioId { get; set; }
    public bool Ativo { get; set; } = true;

    // Propriedades de exibição (preenchidas via JOIN)
    public string ClienteNome { get; set; } = string.Empty;
    public string FormaPagamentoNome { get; set; } = string.Empty;
    public string UsuarioNome { get; set; } = string.Empty;
}
