using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Forms;
using ClientePagamentoSystem.Helpers;
using Microsoft.Extensions.Configuration;

namespace ClientePagamentoSystem;

public static class Program
{
    public static IConfiguration Configuration { get; set; } = null!;
    public static string ConnectionString { get; set; } = string.Empty;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            ErrorHelper.ShowError("Erro inesperado na aplicação.", e.Exception);

        // 1. Carregar configuração
        try
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            ConnectionString = Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "ConnectionString 'DefaultConnection' não encontrada no appsettings.json.");
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError(
                "Não foi possível carregar a configuração.\n\nVerifique o arquivo 'appsettings.json' (ConnectionStrings → DefaultConnection).",
                ex);
            return;
        }

        // 2. Verificar SQL Server / criar banco / tabelas / seeds
        try
        {
            var connection = new DatabaseConnection(ConnectionString);
            var initializer = new DatabaseInitializer(connection);
            initializer.Initialize();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError(
                "Não foi possível conectar ao banco de dados.\n\n" +
                "Verifique:\n" +
                "• se o SQL Server está funcionando (serviço SQL Server / SQLEXPRESS);\n" +
                "• se a instância está correta;\n" +
                "• se a DefaultConnection no 'appsettings.json' está correta.",
                ex);
            // Não fecha em silêncio: oferece tentar novamente
            var retry = MessageBox.Show(
                "Deseja tentar conectar novamente?",
                "Banco de dados indisponível",
                MessageBoxButtons.RetryCancel,
                MessageBoxIcon.Question);
            if (retry == DialogResult.Retry)
            {
                Main();
                return;
            }
            return;
        }

        // 3. Abrir Login
        Application.Run(new LoginForm());
    }
}
