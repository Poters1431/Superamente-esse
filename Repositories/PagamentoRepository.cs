using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Models;
using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Repositories;

public class PagamentoRepository
{
    private readonly IDatabaseConnection _db;

    public PagamentoRepository(IDatabaseConnection db) => _db = db;

    private const string SelectBase = @"
        SELECT p.Id, p.ClienteId, p.Valor, p.DataVencimento, p.DataPagamento,
               p.FormaPagamentoId, p.Status, p.Observacao, p.UsuarioId, p.Ativo,
               c.NomeCompleto AS ClienteNome, f.Nome AS FormaPagamentoNome, u.NomeCompleto AS UsuarioNome
        FROM Pagamentos p
        INNER JOIN Clientes c ON c.Id = p.ClienteId
        INNER JOIN FormasPagamento f ON f.Id = p.FormaPagamentoId
        INNER JOIN Usuarios u ON u.Id = p.UsuarioId";

    public List<Pagamento> Listar(string? filtro = null, int? clienteId = null,
        string? status = null, int? formaPagamentoId = null, bool incluirInativos = false)
    {
        var lista = new List<Pagamento>();
        using var cn = _db.CreateConnection();
        cn.Open();

        var sql = SelectBase + " WHERE 1=1";
        if (!incluirInativos) sql += " AND p.Ativo = 1";
        if (clienteId.HasValue) sql += " AND p.ClienteId = @ClienteId";
        if (!string.IsNullOrWhiteSpace(status)) sql += " AND p.Status = @Status";
        if (formaPagamentoId.HasValue) sql += " AND p.FormaPagamentoId = @FormaPagamentoId";
        if (!string.IsNullOrWhiteSpace(filtro))
            sql += " AND (c.NomeCompleto LIKE @Filtro OR p.Observacao LIKE @Filtro)";
        sql += " ORDER BY p.DataVencimento DESC, p.Id DESC;";

        using var cmd = new SqlCommand(sql, cn);
        if (clienteId.HasValue) cmd.Parameters.AddWithValue("@ClienteId", clienteId.Value);
        if (!string.IsNullOrWhiteSpace(status)) cmd.Parameters.AddWithValue("@Status", status);
        if (formaPagamentoId.HasValue) cmd.Parameters.AddWithValue("@FormaPagamentoId", formaPagamentoId.Value);
        if (!string.IsNullOrWhiteSpace(filtro)) cmd.Parameters.AddWithValue("@Filtro", "%" + filtro + "%");

        using var rd = cmd.ExecuteReader();
        while (rd.Read()) lista.Add(Map(rd));
        return lista;
    }

    public Pagamento? ObterPorId(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        var sql = SelectBase + " WHERE p.Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? Map(rd) : null;
    }

    public int Inserir(Pagamento p)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            INSERT INTO Pagamentos (ClienteId, Valor, DataVencimento, DataPagamento,
                                    FormaPagamentoId, Status, Observacao, UsuarioId, Ativo)
            VALUES (@ClienteId, @Valor, @DataVencimento, @DataPagamento,
                    @FormaPagamentoId, @Status, @Observacao, @UsuarioId, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var cmd = new SqlCommand(sql, cn);
        PreencherParametros(cmd, p);
        return (int)cmd.ExecuteScalar()!;
    }

    public void Atualizar(Pagamento p)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            UPDATE Pagamentos SET ClienteId = @ClienteId, Valor = @Valor,
                DataVencimento = @DataVencimento, DataPagamento = @DataPagamento,
                FormaPagamentoId = @FormaPagamentoId, Status = @Status,
                Observacao = @Observacao
            WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        PreencherParametros(cmd, p);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Desativa (exclusão lógica) — preserva histórico.</summary>
    public void Desativar(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE Pagamentos SET Ativo = 0 WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public DashboardResumo ObterResumo()
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            SELECT
                COUNT(1),
                SUM(CASE WHEN Status = 'Pago' THEN 1 ELSE 0 END),
                SUM(CASE WHEN Status = 'Pendente' THEN 1 ELSE 0 END),
                SUM(CASE WHEN Status = 'Atrasado' THEN 1 ELSE 0 END),
                ISNULL(SUM(CASE WHEN Status = 'Pago' THEN Valor ELSE 0 END), 0),
                ISNULL(SUM(CASE WHEN Status <> 'Pago' THEN Valor ELSE 0 END), 0)
            FROM Pagamentos WHERE Ativo = 1;";
        using var cmd = new SqlCommand(sql, cn);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return new DashboardResumo();
        return new DashboardResumo
        {
            TotalPagamentos = rd.GetInt32(0),
            Pagos = rd.GetInt32(1),
            Pendentes = rd.GetInt32(2),
            Atrasados = rd.GetInt32(3),
            ValorRecebido = rd.GetDecimal(4),
            ValorPendente = rd.GetDecimal(5)
        };
    }

    private static void PreencherParametros(SqlCommand cmd, Pagamento p)
    {
        cmd.Parameters.AddWithValue("@ClienteId", p.ClienteId);
        cmd.Parameters.AddWithValue("@Valor", p.Valor);
        cmd.Parameters.AddWithValue("@DataVencimento", p.DataVencimento.Date);
        cmd.Parameters.AddWithValue("@DataPagamento", (object?)p.DataPagamento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FormaPagamentoId", p.FormaPagamentoId);
        cmd.Parameters.AddWithValue("@Status", p.Status);
        cmd.Parameters.AddWithValue("@Observacao", (object?)p.Observacao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UsuarioId", p.UsuarioId);
    }

    private static Pagamento Map(SqlDataReader rd) => new()
    {
        Id = rd.GetInt32(0),
        ClienteId = rd.GetInt32(1),
        Valor = rd.GetDecimal(2),
        DataVencimento = rd.GetDateTime(3),
        DataPagamento = rd.IsDBNull(4) ? null : rd.GetDateTime(4),
        FormaPagamentoId = rd.GetInt32(5),
        Status = rd.GetString(6),
        Observacao = rd.IsDBNull(7) ? null : rd.GetString(7),
        UsuarioId = rd.GetInt32(8),
        Ativo = rd.GetBoolean(9),
        ClienteNome = rd.GetString(10),
        FormaPagamentoNome = rd.GetString(11),
        UsuarioNome = rd.GetString(12)
    };
}

public class DashboardResumo
{
    public int TotalPagamentos { get; set; }
    public int Pagos { get; set; }
    public int Pendentes { get; set; }
    public int Atrasados { get; set; }
    public decimal ValorRecebido { get; set; }
    public decimal ValorPendente { get; set; }
}
