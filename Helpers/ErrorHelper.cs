namespace ClientePagamentoSystem.Helpers;

/// <summary>
/// Exibe erros com mensagem amigável + detalhe técnico completo (ex.ToString()),
/// e registra em arquivo de log (logs/erros.log) junto ao executável.
/// </summary>
public static class ErrorHelper
{
    public static bool ModoDesenvolvimento { get; set; } = true;

    public static void ShowError(string mensagemAmigavel, Exception ex, IWin32Window? owner = null)
    {
        RegistrarLog(mensagemAmigavel, ex);

        string texto = mensagemAmigavel;
        if (ModoDesenvolvimento)
            texto += "\n\n──────── Detalhes técnicos (desenvolvimento) ────────\n" + ex;

        MessageBox.Show(owner, texto, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void ShowWarning(string mensagem, IWin32Window? owner = null)
        => MessageBox.Show(owner, mensagem, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void ShowSuccess(string mensagem, IWin32Window? owner = null)
        => MessageBox.Show(owner, mensagem, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void RegistrarLog(string mensagem, Exception ex)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var arquivo = Path.Combine(dir, "erros.log");
            File.AppendAllText(arquivo,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensagem}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Log é best-effort: nunca deve derrubar a aplicação.
        }
    }
}
