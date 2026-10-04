using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Data;

public class DatabaseConnection : IDatabaseConnection
{
    public string ConnectionString { get; }

    public DatabaseConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string não pode ser vazia.", nameof(connectionString));
        ConnectionString = connectionString;
    }

    public SqlConnection CreateConnection() => new(ConnectionString);

    /// <summary>Conexão apontando para o banco master (usada para criar o banco).</summary>
    public SqlConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = "master"
        };
        return new SqlConnection(builder.ConnectionString);
    }
}
