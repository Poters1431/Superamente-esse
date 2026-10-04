using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Repositories;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

public class LoginForm : Form
{
    private TextBox txtUsuario = null!;
    private TextBox txtSenha = null!;
    private Button btnEntrar = null!;
    private Button btnMostrarSenha = null!;
    private Label lblErro = null!;

    public LoginForm()
    {
        ConstruirInterface();
    }

    private void ConstruirInterface()
    {
        Text = "ClientePagamentoSystem — Login";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 480);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = UITheme.Background;
        Font = UITheme.FontNormal;

        var card = UITheme.CriarCard();
        card.SetBounds(40, 50, 340, 380);

        var lblTitulo = new Label
        {
            Text = "💳 Bem-vindo",
            Font = UITheme.FontTitulo,
            ForeColor = UITheme.Primary,
            AutoSize = true,
            Location = new Point(24, 24)
        };
        var lblSub = new Label
        {
            Text = "Sistema de Clientes e Pagamentos",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            AutoSize = true,
            Location = new Point(24, 66)
        };

        var lblUsuario = UITheme.CriarLabel("👤 Usuário");
        lblUsuario.Location = new Point(24, 110);
        txtUsuario = UITheme.CriarTextBox("Digite seu usuário");
        txtUsuario.SetBounds(24, 134, 292, 30);
        txtUsuario.Text = "Larissa"; // pré-preenchido para testes

        var lblSenha = UITheme.CriarLabel("🔒 Senha");
        lblSenha.Location = new Point(24, 178);
        txtSenha = UITheme.CriarTextBox("Digite sua senha");
        txtSenha.SetBounds(24, 202, 252, 30);
        txtSenha.UseSystemPasswordChar = true;
        txtSenha.Text = "Larissa123"; // pré-preenchido para testes

        btnMostrarSenha = new Button
        {
            Text = "👁",
            Font = UITheme.FontNormal,
            FlatStyle = FlatStyle.Flat,
            Bounds = new Rectangle(280, 202, 36, 30),
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnMostrarSenha.FlatAppearance.BorderSize = 1;
        btnMostrarSenha.FlatAppearance.BorderColor = UITheme.Border;
        btnMostrarSenha.Click += (_, _) =>
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;
            btnMostrarSenha.Text = txtSenha.UseSystemPasswordChar ? "👁" : "🚫";
        };

        btnEntrar = UITheme.CriarBotao("Entrar");
        btnEntrar.SetBounds(24, 252, 292, 42);
        btnEntrar.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        btnEntrar.Click += BtnEntrar_Click;

        lblErro = new Label
        {
            ForeColor = UITheme.Danger,
            Font = UITheme.FontPequena,
            AutoSize = false,
            Bounds = new Rectangle(24, 304, 292, 50),
            TextAlign = ContentAlignment.TopCenter,
            Visible = false
        };

        card.Controls.AddRange(new Control[]
        {
            lblTitulo, lblSub, lblUsuario, txtUsuario, lblSenha, txtSenha,
            btnMostrarSenha, btnEntrar, lblErro
        });
        Controls.Add(card);

        AcceptButton = btnEntrar;
    }

    private void BtnEntrar_Click(object? sender, EventArgs e)
    {
        lblErro.Visible = false;
        btnEntrar.Enabled = false;
        btnEntrar.Text = "Entrando...";
        Cursor = Cursors.WaitCursor;
        try
        {
            var db = new DatabaseConnection(Program.ConnectionString);
            var auth = new AuthService(new UsuarioRepository(db));

            var usuario = auth.Autenticar(txtUsuario.Text, txtSenha.Text);

            Hide();
            using var main = new MainForm(usuario);
            main.ShowDialog();
            Close();
        }
        catch (UnauthorizedAccessException ex)
        {
            lblErro.Text = "⚠ " + ex.Message;
            lblErro.Visible = true;
        }
        catch (ArgumentException ex)
        {
            lblErro.Text = "⚠ " + ex.Message;
            lblErro.Visible = true;
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError(
                "Não foi possível realizar o login.\n\nVerifique a conexão com o banco de dados e tente novamente.",
                ex, this);
        }
        finally
        {
            btnEntrar.Enabled = true;
            btnEntrar.Text = "Entrar";
            Cursor = Cursors.Default;
        }
    }
}
