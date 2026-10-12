using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Models;
using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Repositories;

public class UsuarioRepository
{
    private readonly IDatabaseConnection _db;

    public UsuarioRepository(IDatabaseConnection db) => _db = db;

    public Usuario? ObterPorUsername(string username)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = @"
            SELECT Id, Username, PasswordHash, NomeCompleto, Email, Ativo,
                   ISNULL(Perfil, 'adm'), DataCriacao, UltimoLogin
            FROM Usuarios WHERE Username = @Username;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Username", username);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return null;

        return new Usuario
        {
            Id = rd.GetInt32(0),
            Username = rd.GetString(1),
            PasswordHash = rd.GetString(2),
            NomeCompleto = rd.GetString(3),
            Email = rd.GetString(4),
            Ativo = rd.GetBoolean(5),
            Perfil = rd.GetString(6),
            DataCriacao = rd.GetDateTime(7),
            UltimoLogin = rd.IsDBNull(8) ? null : rd.GetDateTime(8)
        };
    }

    public void RegistrarLogin(int usuarioId)
    {
        using var cn = _db.CreateConnection();
        cn.Open();
        const string sql = "UPDATE Usuarios SET UltimoLogin = GETDATE() WHERE Id = @Id;";
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", usuarioId);
        cmd.ExecuteNonQuery();
    }
}
