using System.Net;
using System.Net.Mail;
using System.Text.Json.Nodes;

namespace ClientePagamentoSystem.Services;

/// <summary>
/// Envia e-mails via SMTP (Gmail, Outlook etc.) e lê/grava a configuração
/// de SMTP no appsettings.json.
/// </summary>
public static class EmailService
{
    public record SmtpConfig(string Host, int Porta, string Usuario, string Senha,
        string Remetente, string NomeRemetente);

    public static SmtpConfig CarregarConfig()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(caminho))
            {
                var json = JsonNode.Parse(File.ReadAllText(caminho));
                return new SmtpConfig(
                    json?["SmtpHost"]?.GetValue<string>() ?? "smtp.gmail.com",
                    json?["SmtpPorta"]?.GetValue<int>() ?? 587,
                    json?["SmtpUsuario"]?.GetValue<string>() ?? "",
                    json?["SmtpSenha"]?.GetValue<string>() ?? "",
                    json?["SmtpRemetente"]?.GetValue<string>() ?? "",
                    json?["SmtpNome"]?.GetValue<string>() ?? "");
            }
        }
        catch { /* ignora */ }
        return new SmtpConfig("smtp.gmail.com", 587, "", "", "", "");
    }

    public static void SalvarConfig(SmtpConfig cfg)
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var json = File.Exists(caminho)
            ? JsonNode.Parse(File.ReadAllText(caminho)) ?? new JsonObject()
            : new JsonObject();
        json["SmtpHost"] = cfg.Host;
        json["SmtpPorta"] = cfg.Porta;
        json["SmtpUsuario"] = cfg.Usuario;
        json["SmtpSenha"] = cfg.Senha;
        json["SmtpRemetente"] = cfg.Remetente;
        json["SmtpNome"] = cfg.NomeRemetente;
        File.WriteAllText(caminho,
            json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Envia e-mail com corpo HTML e anexo opcional.</summary>
    public static void Enviar(SmtpConfig cfg, string para, string assunto,
        string corpoHtml, string? caminhoAnexo = null)
    {
        using var msg = new MailMessage
        {
            From = new MailAddress(
                string.IsNullOrWhiteSpace(cfg.Remetente) ? cfg.Usuario : cfg.Remetente,
                string.IsNullOrWhiteSpace(cfg.NomeRemetente) ? cfg.Usuario : cfg.NomeRemetente),
            Subject = assunto,
            Body = corpoHtml,
            IsBodyHtml = true
        };
        msg.To.Add(para);
        if (!string.IsNullOrWhiteSpace(caminhoAnexo) && File.Exists(caminhoAnexo))
            msg.Attachments.Add(new Attachment(caminhoAnexo));

        using var smtp = new SmtpClient(cfg.Host, cfg.Porta)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(cfg.Usuario, cfg.Senha),
            Timeout = 30000
        };
        smtp.Send(msg);
    }
}
