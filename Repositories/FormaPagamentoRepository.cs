using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Models;
using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Repositories;

public class FormaPagamentoRepository
{
    private readonly IDatabaseConnection _db;

    public FormaPagamentoRepository(IDatabaseConnection db) => _db = db;

    public List<FormaPagamento> Listar(bool somenteAtivos = false)
    {
        var lista = new List<FormaPagamento>();
        using var cn = _db.CreateConnection();
        cn.Open();
        var sql = "SELECT Id, Nome, Descricao, Ativo FROM FormasPagamento"
                + (somenteAtivos ? " WHERE Ativo = 1" : "")
                + " ORDER BY Nome;";
        using var cmd = new SqlCommand(sql, cn);
        using var rd = cmd.ExecuteReader();
        while (rd.Read())
        {
            lista.Add(new FormaPagamento
            {
                Id = rd.GetInt32(0),
                Nome = rd.GetString(1),
                Descricao = rd.IsDBNull(2) ? null : rd.GetString(2),
                Ativo = rd.GetBoolean(3)
            });
        }
        return lista;
    }

    public FormaPagamento? ObterPorId(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "SELECT Id, Nome, Descricao, Ativo FROM FormasPagamento WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;
        return new FormaPagamento
        {
            Id = rd.GetInt32(0),
            Nome = rd.GetString(1),
            Descricao = rd.IsDBNull(2) ? null : rd.GetString(2),
            Ativo = rd.GetBoolean(3)
        };
    }

    public int Inserir(FormaPagamento f)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            INSERT INTO FormasPagamento (Nome, Descricao, Ativo) VALUES (@Nome, @Descricao, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Nome", f.Nome);
        cmd.Parameters.AddWithValue("@Descricao", (object?)f.Descricao ?? DBNull.Value);
        return (int)cmd.ExecuteScalar()!;
    }

    public void Atualizar(FormaPagamento f)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE FormasPagamento SET Nome = @Nome, Descricao = @Descricao WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Nome", f.Nome);
        cmd.Parameters.AddWithValue("@Descricao", (object?)f.Descricao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", f.Id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Desativa a forma. Pagamentos históricos NÃO são apagados.</summary>
    public void Desativar(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE FormasPagamento SET Ativo = 0 WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void Reativar(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE FormasPagamento SET Ativo = 1 WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }
}
