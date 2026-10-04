using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Models;
using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Repositories;

public class ClienteRepository
{
    private readonly IDatabaseConnection _db;

    public ClienteRepository(IDatabaseConnection db) => _db = db;

    public List<Cliente> Listar(string? filtro = null, bool somenteAtivos = false)
    {
        var lista = new List<Cliente>();
        using var cn = _db.CreateConnection();
        cn.Open();

        var sql = @"SELECT Id, NomeCompleto, CPF, Telefone, Email, DataNascimento, Endereco, Ativo
                    FROM Clientes WHERE 1=1";
        if (somenteAtivos) sql += " AND Ativo = 1";
        if (!string.IsNullOrWhiteSpace(filtro))
            sql += " AND (NomeCompleto LIKE @Filtro OR CPF LIKE @Filtro OR Email LIKE @Filtro)";
        sql += " ORDER BY NomeCompleto;";

        using var cmd = new SqlCommand(sql, cn);
        if (!string.IsNullOrWhiteSpace(filtro))
            cmd.Parameters.AddWithValue("@Filtro", "%" + filtro + "%");

        using var rd = cmd.ExecuteReader();
        while (rd.Read()) lista.Add(Map(rd));
        return lista;
    }

    public Cliente? ObterPorId(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"SELECT Id, NomeCompleto, CPF, Telefone, Email, DataNascimento, Endereco, Ativo
                             FROM Clientes WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var rd = cmd.ExecuteReader();
        return rd.Read() ? Map(rd) : null;
    }

    public bool ExisteCpf(string cpf, int ignorarId = 0)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE CPF = @CPF AND Id <> @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@CPF", cpf);
        cmd.Parameters.AddWithValue("@Id", ignorarId);
        return (int)cmd.ExecuteScalar()! > 0;
    }

    public int Inserir(Cliente c)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            INSERT INTO Clientes (NomeCompleto, CPF, Telefone, Email, DataNascimento, Endereco, Ativo)
            VALUES (@Nome, @CPF, @Telefone, @Email, @DataNascimento, @Endereco, 1);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var cmd = new SqlCommand(sql, cn);
        PreencherParametros(cmd, c);
        return (int)cmd.ExecuteScalar()!;
    }

    public void Atualizar(Cliente c)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            UPDATE Clientes SET NomeCompleto = @Nome, CPF = @CPF, Telefone = @Telefone,
                Email = @Email, DataNascimento = @DataNascimento, Endereco = @Endereco
            WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        PreencherParametros(cmd, c);
        cmd.Parameters.AddWithValue("@Id", c.Id);
        cmd.ExecuteNonQuery();
    }

    public void Desativar(int id)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE Clientes SET Ativo = 0 WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public int Contar(bool somenteAtivos = true)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        var sql = "SELECT COUNT(1) FROM Clientes" + (somenteAtivos ? " WHERE Ativo = 1;" : ";");
        using var cmd = new SqlCommand(sql, cn);
        return (int)cmd.ExecuteScalar()!;
    }

    private static void PreencherParametros(SqlCommand cmd, Cliente c)
    {
        cmd.Parameters.AddWithValue("@Nome", c.NomeCompleto);
        cmd.Parameters.AddWithValue("@CPF", c.CPF);
        cmd.Parameters.AddWithValue("@Telefone", (object?)c.Telefone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)c.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DataNascimento", (object?)c.DataNascimento ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Endereco", (object?)c.Endereco ?? DBNull.Value);
    }

    private static Cliente Map(SqlDataReader rd) => new()
    {
        Id = rd.GetInt32(0),
        NomeCompleto = rd.GetString(1),
        CPF = rd.GetString(2),
        Telefone = rd.IsDBNull(3) ? null : rd.GetString(3),
        Email = rd.IsDBNull(4) ? null : rd.GetString(4),
        DataNascimento = rd.IsDBNull(5) ? null : rd.GetDateTime(5),
        Endereco = rd.IsDBNull(6) ? null : rd.GetString(6),
        Ativo = rd.GetBoolean(7)
    };
}
