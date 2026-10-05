using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

public class ClientesView : UserControl
{
    private DataGridView grid = null!;
    private TextBox txtBusca = null!;
    private Button btnNovo = null!, btnEditar = null!, btnDesativar = null!, btnBuscar = null!, btnReativar = null!, btnExtrato = null!;

    private ClienteService _service = null!;
    private PagamentoService _pagService = null!;
    private FormaPagamentoRepository _formaRepo = null!;
    private readonly Usuario _usuario;

    // Painel financeiro do cliente selecionado
    private Label lblFinNome = null!, lblFinTotal = null!, lblFinPago = null!,
                  lblFinRestante = null!, lblFinParcelas = null!, lblFinStatus = null!;

    public ClientesView(Usuario usuario)
    {
        _usuario = usuario;
        ConstruirInterface();
        Load += (_, _) => Inicializar();
    }

    private void ConstruirInterface()
    {
        BackColor = UITheme.Background;

        var lblTitulo = new Label
        {
            Text = "👥 Clientes",
            Font = UITheme.FontTitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 50
        };

        // Barra de ferramentas
        // Barra de ferramentas dentro de um card branco para dar contraste
        var cardToolbar = UITheme.CriarCard();
        cardToolbar.Dock = DockStyle.Top;
        cardToolbar.Height = 72;
        cardToolbar.Padding = new Padding(12, 10, 12, 10);

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Card };

        txtBusca = UITheme.CriarTextBox("Pesquisar por nome, CPF ou e-mail...");
        txtBusca.Width = 320;
        txtBusca.Anchor = AnchorStyles.Left | AnchorStyles.Top;

        btnBuscar = UITheme.CriarBotao("🔍 Buscar", null, true);
        btnBuscar.Width = 100;
        btnBuscar.Click += (_, _) => CarregarGrid();
        txtBusca.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CarregarGrid(); e.SuppressKeyPress = true; } };

        btnNovo = UITheme.CriarBotao("➕ Novo Cliente");
        btnNovo.Width = 140;
        btnNovo.Click += (_, _) => AbrirFormulario(null);

        btnEditar = UITheme.CriarBotao("✏ Editar", UITheme.Warning);
        btnEditar.Width = 100;
        btnEditar.Click += (_, _) => EditarSelecionado();

        btnDesativar = UITheme.CriarBotao("🗑 Desativar", UITheme.Danger);
        btnDesativar.Width = 110;
        btnDesativar.Click += (_, _) => DesativarSelecionado();

        btnReativar = UITheme.CriarBotao("♻ Reativar", UITheme.Warning);
        btnReativar.Width = 110;
        btnReativar.Click += (_, _) => ReativarSelecionado();

        btnExtrato = UITheme.CriarBotao("📄 Extrato", UITheme.PrimaryDark);
        btnExtrato.Width = 110;
        btnExtrato.Click += (_, _) => GerarExtratoDoSelecionado();

        toolbar.Controls.Add(txtBusca);
        toolbar.Controls.Add(btnBuscar);
        toolbar.Controls.Add(btnNovo);
        toolbar.Controls.Add(btnEditar);
        toolbar.Controls.Add(btnDesativar);
        toolbar.Controls.Add(btnReativar);
        toolbar.Controls.Add(btnExtrato);
        toolbar.Resize += (_, _) => PosicionarToolbar(toolbar);
        PosicionarToolbar(toolbar);
        cardToolbar.Controls.Add(toolbar);

        // Grid
        grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 30 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome", FillWeight = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CPF", HeaderText = "CPF", FillWeight = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Telefone", HeaderText = "Telefone", FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-mail", FillWeight = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nascimento", HeaderText = "Nascimento", FillWeight = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 45 });
        grid.CellDoubleClick += (_, _) => EditarSelecionado();
        grid.SelectionChanged += (_, _) =>
        {
            AtualizarPainelFinanceiro();
            AtualizarEstadoBotoes();
        };

        // ---------- Painel financeiro (Total a pagar × Pago × Restante) ----------
        var painelFin = UITheme.CriarCard();
        painelFin.Dock = DockStyle.Bottom;
        painelFin.Height = 96;
        painelFin.Padding = new Padding(16, 12, 16, 12);

        var finLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1
        };
        finLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24f));
        for (int i = 0; i < 5; i++)
            finLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.2f));

        (Label titulo, Label valor) Bloco(string tituloTxt, Color cor)
        {
            var p = new Panel { Dock = DockStyle.Fill };
            var lt = new Label
            {
                Text = tituloTxt,
                Font = UITheme.FontPequena,
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 20
            };
            var lv = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = cor,
                Dock = DockStyle.Fill
            };
            p.Controls.Add(lv);
            p.Controls.Add(lt);
            return (lt, lv);
        }

        var bNome = Bloco("Cliente selecionado", UITheme.Primary);
        var bTotal = Bloco("💰 Total a pagar", UITheme.Text);
        var bPago = Bloco("✅ Total pago", UITheme.Success);
        var bRestante = Bloco("⏳ Restante", UITheme.Warning);
        var bParcelas = Bloco("Parcelas (pagas/total)", UITheme.Text);
        var bStatus = Bloco("Situação", UITheme.PrimaryDark);

        lblFinNome = bNome.valor;
        lblFinTotal = bTotal.valor;
        lblFinPago = bPago.valor;
        lblFinRestante = bRestante.valor;
        lblFinParcelas = bParcelas.valor;
        lblFinStatus = bStatus.valor;

        foreach (var (t, _) in new[] { bNome, bTotal, bPago, bRestante, bParcelas, bStatus })
            finLayout.Controls.Add(t.Parent!);

        painelFin.Controls.Add(finLayout);

        var cardGrid = UITheme.CriarCard();
        cardGrid.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(painelFin);   // Bottom
        Controls.Add(cardToolbar); // Top
        Controls.Add(lblTitulo);   // Top (docked primeiro)
    }

    private void PosicionarToolbar(Panel toolbar)
    {
        int x = 0;
        foreach (Control c in new Control[] { txtBusca, btnBuscar, btnNovo, btnEditar, btnDesativar, btnReativar, btnExtrato })
        {
            c.Location = new Point(x, (toolbar.Height - c.Height) / 2);
            x += c.Width + 10;
        }
    }

    private void Inicializar()
    {
        try
        {
            var db = new DatabaseConnection(Program.ConnectionString);
            var clienteRepo = new ClienteRepository(db);
            _service = new ClienteService(clienteRepo);
            _formaRepo = new FormaPagamentoRepository(db);
            _pagService = new PagamentoService(
                new PagamentoRepository(db), clienteRepo, _formaRepo);
            CarregarGrid();
            AtualizarPainelFinanceiro();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar os clientes.", ex, this);
        }
    }

    private void CarregarGrid()
    {
        try
        {
            var clientes = _service.Listar(txtBusca.Text.Trim());
            grid.Rows.Clear();
            foreach (var c in clientes)
            {
                grid.Rows.Add(c.Id, c.NomeCompleto, ValidationHelper.FormatarCpf(c.CPF),
                    c.Telefone ?? "", c.Email ?? "",
                    c.DataNascimento?.ToString("dd/MM/yyyy") ?? "",
                    c.Ativo ? "Ativo" : "Inativo");
            }
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao carregar a lista de clientes.", ex, this);
        }
    }

    private Cliente? Selecionado()
    {
        if (grid.SelectedRows.Count == 0) return null;
        var id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
        return _service.ObterPorId(id);
    }

    private void EditarSelecionado()
    {
        var cliente = Selecionado();
        if (cliente is null)
        {
            ErrorHelper.ShowWarning("Selecione um cliente na lista.", this);
            return;
        }
        AbrirFormulario(cliente);
    }

    private void AbrirFormulario(Cliente? cliente)
    {
        using var form = new ClienteForm(_service, cliente);
        if (form.ShowDialog(this) == DialogResult.OK)
            CarregarGrid();
    }

    private void AtualizarPainelFinanceiro()
    {
        if (lblFinNome is null) return;
        var cliente = Selecionado();
        if (cliente is null || _pagService is null)
        {
            lblFinNome.Text = "—";
            lblFinTotal.Text = "—";
            lblFinPago.Text = "—";
            lblFinRestante.Text = "—";
            lblFinParcelas.Text = "—";
            lblFinStatus.Text = "—";
            return;
        }
        try
        {
            var r = _pagService.ObterResumoCliente(cliente.Id);
            lblFinNome.Text = cliente.NomeCompleto;
            lblFinTotal.Text = r.ValorTotal.ToString("C2");
            lblFinPago.Text = r.TotalPago.ToString("C2");
            lblFinRestante.Text = r.ValorRestante.ToString("C2");
            lblFinParcelas.Text = $"{r.ParcelasPagas}/{r.QuantidadeParcelas}";
            lblFinStatus.Text = r.Status;
            lblFinStatus.ForeColor = r.Status switch
            {
                "Quitado" => UITheme.Success,
                "Parcialmente pago" => UITheme.Warning,
                "Em aberto" => UITheme.Danger,
                _ => UITheme.TextMuted
            };
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowWarning("Não foi possível calcular o resumo financeiro: " + ex.Message, this);
        }
    }

    /// <summary>
    /// Habilita/desabilita os botões conforme o cliente selecionado:
    /// Desativar só para clientes ativos, Reativar só para inativos.
    /// </summary>
    private void AtualizarEstadoBotoes()
    {
        var cliente = Selecionado();
        btnDesativar.Enabled = cliente is not null && cliente.Ativo;
        btnReativar.Enabled = cliente is not null && !cliente.Ativo;
    }

    /// <summary>
    /// Gera o extrato/recibo do cliente selecionado em HTML e abre no navegador
    /// (de lá dá para imprimir ou salvar em PDF com Ctrl+P).
    /// </summary>
    private void GerarExtratoDoSelecionado()
    {
        var cliente = Selecionado();
        if (cliente is null)
        {
            ErrorHelper.ShowWarning("Selecione um cliente na lista para gerar o extrato.", this);
            return;
        }
        try
        {
            var resumo = _pagService.ObterResumoCliente(cliente.Id);
            var pagamentos = _pagService.Listar(clienteId: cliente.Id, somenteClientesAtivos: false);
            ExtratoService.AbrirExtrato(cliente, resumo, pagamentos, _usuario.NomeCompleto);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao gerar o extrato do cliente.", ex, this);
        }
    }

    private void ReativarSelecionado()
    {
        var cliente = Selecionado();
        if (cliente is null)
        {
            ErrorHelper.ShowWarning("Selecione um cliente na lista.", this);
            return;
        }
        if (cliente.Ativo)
        {
            ErrorHelper.ShowWarning("Este cliente já está ativo.", this);
            return;
        }
        if (MessageBox.Show(this, $"Reativar o cliente '{cliente.NomeCompleto}'?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            _service.Reativar(cliente.Id);
            ErrorHelper.ShowSuccess("Cliente reativado com sucesso.", this);
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao reativar o cliente.", ex, this);
        }
    }

    private void DesativarSelecionado()
    {
        var cliente = Selecionado();
        if (cliente is null)
        {
            ErrorHelper.ShowWarning("Selecione um cliente na lista.", this);
            return;
        }
        if (MessageBox.Show(this, $"Desativar o cliente '{cliente.NomeCompleto}'?\n(O histórico de pagamentos será mantido.)",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            _service.Desativar(cliente.Id);
            ErrorHelper.ShowSuccess("Cliente desativado com sucesso.", this);
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao desativar o cliente.", ex, this);
        }
    }
}
