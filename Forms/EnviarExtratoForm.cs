using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>
/// Diálogo para enviar o extrato do cliente por e-mail.
/// Configuração de SMTP (Gmail, Outlook...) salva no appsettings.json.
/// </summary>
public class EnviarExtratoForm : Form
{
    private readonly Cliente _cliente;
    private readonly ResumoFinanceiroCliente _resumo;
    private readonly List<Pagamento> _pagamentos;
    private readonly string _nomeUsuario;

    private TextBox txtPara = null!, txtHost = null!, txtPorta = null!,
                    txtUsuario = null!, txtSenha = null!, txtNome = null!;
    private Label lblStatus = null!;

    public EnviarExtratoForm(Cliente cliente, ResumoFinanceiroCliente resumo,
        List<Pagamento> pagamentos, string nomeUsuario)
    {
        _cliente = cliente;
        _resumo = resumo;
        _pagamentos = pagamentos;
        _nomeUsuario = nomeUsuario;
        Construir();
    }

    private void Construir()
    {
        Text = "✉ Enviar extrato por e-mail";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Card;
        ClientSize = new Size(460, 470);

        var lblTitulo = UITheme.CriarLabel("✉ Enviar extrato por e-mail", bold: true,
            cor: UITheme.Primary);
        lblTitulo.SetBounds(20, 14, 420, 24);

        var lblPara = UITheme.CriarLabel("Enviar para (e-mail do cliente):");
        lblPara.SetBounds(20, 46, 420, 20);
        txtPara = UITheme.CriarTextBox();
        txtPara.SetBounds(20, 68, 420, 28);
        txtPara.Text = _cliente.Email ?? "";

        var lblDica = new Label
        {
            Text = "Configuração do seu e-mail (fica salva):",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Bounds = new Rectangle(20, 108, 420, 20)
        };

        var lblHost = UITheme.CriarLabel("Servidor SMTP / porta:");
        lblHost.SetBounds(20, 132, 420, 20);
        txtHost = UITheme.CriarTextBox("smtp.gmail.com");
        txtHost.SetBounds(20, 154, 270, 28);
        txtPorta = UITheme.CriarTextBox("587");
        txtPorta.SetBounds(298, 154, 100, 28);
        txtPorta.TextAlign = HorizontalAlignment.Center;

        var lblUsuario = UITheme.CriarLabel("Seu e-mail:");
        lblUsuario.SetBounds(20, 190, 420, 20);
        txtUsuario = UITheme.CriarTextBox();
        txtUsuario.SetBounds(20, 212, 420, 28);

        var lblSenha = UITheme.CriarLabel("Senha de app (Gmail: gerar em myaccount.google.com/apppasswords):");
        lblSenha.SetBounds(20, 248, 420, 20);
        txtSenha = UITheme.CriarTextBox();
        txtSenha.SetBounds(20, 270, 420, 28);
        txtSenha.UseSystemPasswordChar = true;

        var lblNome = UITheme.CriarLabel("Nome do remetente:");
        lblNome.SetBounds(20, 306, 420, 20);
        txtNome = UITheme.CriarTextBox();
        txtNome.SetBounds(20, 328, 420, 28);

        lblStatus = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Bounds = new Rectangle(20, 366, 420, 20)
        };

        var btnSalvar = UITheme.CriarBotao("💾 Salvar config", UITheme.PrimaryDark);
        btnSalvar.SetBounds(20, 396, 130, 36);
        btnSalvar.Click += (_, _) =>
        {
            try
            {
                EmailService.SalvarConfig(LerConfig());
                lblStatus.ForeColor = UITheme.Success;
                lblStatus.Text = "Configuração salva com sucesso.";
            }
            catch (Exception ex)
            {
                ErrorHelper.ShowError("Não foi possível salvar a configuração.", ex, this);
            }
        };

        var btnEnviar = UITheme.CriarBotao("📧 Enviar extrato", UITheme.Success);
        btnEnviar.SetBounds(160, 396, 160, 36);
        btnEnviar.Click += BtnEnviar_Click;

        var btnFechar = UITheme.CriarBotao("Fechar", null, true);
        btnFechar.SetBounds(328, 396, 92, 36);
        btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblPara, txtPara, lblDica, lblHost, txtHost, txtPorta,
            lblUsuario, txtUsuario, lblSenha, txtSenha, lblNome, txtNome,
            lblStatus, btnSalvar, btnEnviar, btnFechar
        });

        AcceptButton = btnEnviar;
        CancelButton = btnFechar;

        var cfg = EmailService.CarregarConfig();
        txtHost.Text = cfg.Host;
        txtPorta.Text = cfg.Porta.ToString();
        txtUsuario.Text = cfg.Usuario;
        txtSenha.Text = cfg.Senha;
        txtNome.Text = cfg.NomeRemetente;
    }

    private EmailService.SmtpConfig LerConfig() => new(
        txtHost.Text.Trim(),
        int.TryParse(txtPorta.Text, out var p) ? p : 587,
        txtUsuario.Text.Trim(),
        txtSenha.Text,
        txtUsuario.Text.Trim(),
        txtNome.Text.Trim());

    private void BtnEnviar_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPara.Text) || !txtPara.Text.Contains('@'))
        {
            ErrorHelper.ShowWarning("Informe um e-mail de destino válido.", this);
            txtPara.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtSenha.Text))
        {
            ErrorHelper.ShowWarning("Preencha seu e-mail e a senha de app para enviar.", this);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            lblStatus.ForeColor = UITheme.TextMuted;
            lblStatus.Text = "Enviando...";
            Refresh();

            var html = ExtratoService.GerarHtml(_cliente, _resumo, _pagamentos, _nomeUsuario);
            var anexo = Path.Combine(Path.GetTempPath(),
                $"extrato_{_cliente.NomeCompleto.Replace(' ', '_')}.html");
            File.WriteAllText(anexo, html);

            EmailService.Enviar(LerConfig(), txtPara.Text.Trim(),
                $"Extrato — {_cliente.NomeCompleto}",
                $"<p>Olá {System.Net.WebUtility.HtmlEncode(_cliente.NomeCompleto)},</p>" +
                "<p>Segue em anexo o seu extrato atualizado. Abra o arquivo no navegador para visualizar ou imprimir.</p>" +
                "<p>Atenciosamente.</p>",
                anexo);

            lblStatus.ForeColor = UITheme.Success;
            lblStatus.Text = "Extrato enviado com sucesso!";
            ErrorHelper.ShowSuccess("Extrato enviado com sucesso!", this);
        }
        catch (Exception ex)
        {
            lblStatus.ForeColor = UITheme.Danger;
            lblStatus.Text = "Falha ao enviar.";
            ErrorHelper.ShowError(
                "Não foi possível enviar o e-mail.\n\n" +
                "Gmail: use uma \"senha de app\" (myaccount.google.com/apppasswords), " +
                "não a senha normal, e o 2FA precisa estar ativo.", ex, this);
        }
        finally { Cursor = Cursors.Default; }
    }
}
