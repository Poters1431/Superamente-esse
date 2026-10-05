using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;

namespace ClientePagamentoSystem.Forms;

/// <summary>Janela principal com menu lateral e área de conteúdo.</summary>
public class MainForm : Form
{
    private readonly Usuario _usuarioLogado;
    private Panel pnlConteudo = null!;
    private readonly List<Button> _botoesMenu = new();
    private string _itemAtual = "Dashboard";

    public MainForm(Usuario usuarioLogado)
    {
        _usuarioLogado = usuarioLogado;
        ConstruirInterface();
        MostrarView("Dashboard");
    }

    private void ConstruirInterface()
    {
        Text = "ClientePagamentoSystem";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 680);
        BackColor = UITheme.Background;

        // ---------- Menu lateral ----------
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = UITheme.Sidebar,
            Padding = new Padding(12)
        };

        var lblLogo = new Label
        {
            Text = "💳 ClientePagamento",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };
        sidebar.Controls.Add(lblLogo);

        string[] itens = { "Dashboard", "Clientes", "Pagamentos", "Formas de Pagamento", "Sair" };
        for (int i = itens.Length - 1; i >= 0; i--)
        {
            var nome = itens[i];
            var btn = new Button
            {
                Text = Icone(nome) + "  " + nome,
                Font = UITheme.FontNormal,
                ForeColor = Color.White,
                BackColor = UITheme.Sidebar,
                FlatStyle = FlatStyle.Flat,
                Dock = i == itens.Length - 1 ? DockStyle.Bottom : DockStyle.Top,
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = nome
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = UITheme.SidebarHover;
            btn.Click += Menu_Click;
            sidebar.Controls.Add(btn);
            if (nome != "Sair") _botoesMenu.Add(btn);
        }

        var lblUsuario = new Label
        {
            Text = $"👤 {_usuarioLogado.NomeCompleto}",
            Font = UITheme.FontPequena,
            ForeColor = Color.FromArgb(191, 219, 254),
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter
        };
        sidebar.Controls.Add(lblUsuario);

        // Botão de alternância de tema (claro/escuro)
        var btnTema = new Button
        {
            Text = UITheme.ModoEscuro ? "☀  Tema claro" : "🌙  Tema escuro",
            Font = UITheme.FontNormal,
            ForeColor = Color.White,
            BackColor = UITheme.Sidebar,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Bottom,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnTema.FlatAppearance.BorderSize = 0;
        btnTema.FlatAppearance.MouseOverBackColor = UITheme.SidebarHover;
        btnTema.Click += (_, _) =>
        {
            UITheme.AplicarTema(!UITheme.ModoEscuro);
            Controls.Clear();
            _botoesMenu.Clear();
            ConstruirInterface();
            MostrarView(_itemAtual);
        };
        sidebar.Controls.Add(btnTema);

        // Botão de backup do banco (.bak)
        var btnBackup = new Button
        {
            Text = "💾  Backup do banco",
            Font = UITheme.FontNormal,
            ForeColor = Color.White,
            BackColor = UITheme.Sidebar,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Bottom,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnBackup.FlatAppearance.BorderSize = 0;
        btnBackup.FlatAppearance.MouseOverBackColor = UITheme.SidebarHover;
        btnBackup.Click += (_, _) => FazerBackup();
        sidebar.Controls.Add(btnBackup);

        // ---------- Conteúdo ----------
        pnlConteudo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.Background,
            Padding = new Padding(20)
        };

        Controls.Add(pnlConteudo);
        Controls.Add(sidebar);
    }

    private static string Icone(string item) => item switch
    {
        "Dashboard" => "📊",
        "Clientes" => "👥",
        "Pagamentos" => "💰",
        "Formas de Pagamento" => "💳",
        "Sair" => "🚪",
        _ => "•"
    };

    private void Menu_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string item) return;

        if (item == "Sair")
        {
            if (MessageBox.Show("Deseja realmente sair do sistema?", "Sair",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
            return;
        }

        MostrarView(item);
    }

    /// <summary>Gera um backup .bak do banco na pasta que o usuário escolher.</summary>
    private void FazerBackup()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Escolha a pasta onde será salvo o backup (.bak) do banco de dados",
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            var arquivo = Services.BackupService.RealizarBackup(dlg.SelectedPath);
            ErrorHelper.ShowSuccess($"Backup criado com sucesso!\n\n{arquivo}", this);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError(
                "Não foi possível fazer o backup.\n\nDica: escolha uma pasta simples " +
                "(ex.: Documentos) — o SQL Server precisa de permissão de escrita nela.", ex, this);
        }
        finally { Cursor = Cursors.Default; }
    }

    private void MostrarView(string item)
    {
        _itemAtual = item;
        foreach (var b in _botoesMenu)
            b.BackColor = (string?)b.Tag == item ? UITheme.SidebarHover : UITheme.Sidebar;

        pnlConteudo.SuspendLayout();
        pnlConteudo.Controls.Clear();

        UserControl view = item switch
        {
            "Dashboard" => new DashboardView(),
            "Clientes" => new ClientesView(_usuarioLogado),
            "Pagamentos" => new PagamentosView(_usuarioLogado),
            "Formas de Pagamento" => new FormasPagamentoView(),
            _ => new DashboardView()
        };
        view.Dock = DockStyle.Fill;
        pnlConteudo.Controls.Add(view);
        pnlConteudo.ResumeLayout();
    }
}
