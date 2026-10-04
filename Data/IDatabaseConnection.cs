using Microsoft.Data.SqlClient;

namespace ClientePagamentoSystem.Data;

public interface IDatabaseConnection
{
    string ConnectionString { get; }
    SqlConnection CreateConnection();
}
