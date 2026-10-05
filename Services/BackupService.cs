using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Services;

/// <summary>
/// Gera um backup completo (.bak) do banco ClientePagamentoDB numa pasta escolhida.
/// </summary>
public static class BackupService
{
    public static string RealizarBackup(string pastaDestino)
    {
        var csb = new SqlConnectionStringBuilder(Program.ConnectionString)
        {
            InitialCatalog = "master"
        };
        var banco = new SqlConnectionStringBuilder(Program.ConnectionString).InitialCatalog;

        var arquivo = Path.Combine(pastaDestino,
            $"{banco}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

        using var cn = new SqlConnection(csb.ConnectionString);
        cn.Open();
        using var cmd = new SqlCommand(
            $"BACKUP DATABASE [{banco}] TO DISK = @Arq WITH INIT, COMPRESSION;", cn);
        cmd.Parameters.AddWithValue("@Arq", arquivo);
        cmd.CommandTimeout = 300;
        cmd.ExecuteNonQuery();
        return arquivo;
    }
}
