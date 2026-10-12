namespace ClientePagamentoSystem.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    /// <summary>"adm" (acesso total) ou "func" (sem Mapa Mensal).</summary>
    public string Perfil { get; set; } = "adm";
    public DateTime DataCriacao { get; set; }
    public DateTime? UltimoLogin { get; set; }
}
