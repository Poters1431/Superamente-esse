using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

public class PagamentosView : UserControl
{
    private readonly Usuario _usuario;

    private DataGridView grid = null!;
    private TextBox txtBusca = null!;
    private ComboBox cmbFiltroCliente = null!, cmbFiltroStatus = null!, cmbFiltroForma = null!;
    private Label lblResumo = null!;

    private PagamentoService _service = null!;
    private ClienteService _clienteService = null!;
    private FormaPagamentoRepository _formaRepo = null!;

    public PagamentosView(Usuario usuario)
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
            Text = "💰 Pagamentos",
            Font = UITheme.FontTitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 50
        };

        // ---------- Filtros ----------
        var filtros = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = UITheme.Background };

        txtBusca = UITheme.CriarTextBox("Pesquisar por cliente ou observação...");
        txtBusca.SetBounds(0, 8, 280, 30);
        txtBusca.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CarregarGrid(); e.SuppressKeyPress = true; } };

        cmbFiltroCliente = UITheme.CriarComboBox();
        cmbFiltroCliente.SetBounds(292, 8, 210, 30);
        cmbFiltroCliente.SelectedIndexChanged += (_, _) => CarregarGrid();

        cmbFiltroStatus = UITheme.CriarComboBox();
        cmbFiltroStatus.SetBounds(514, 8, 130, 30);
        cmbFiltroStatus.Items.AddRange(new object[] { "(Todos os status)", "Pendente", "Pago", "Atrasado" });
        cmbFiltroStatus.SelectedIndex = 0;
        cmbFiltroStatus.SelectedIndexChanged += (_, _) => CarregarGrid();

        cmbFiltroForma = UITheme.CriarComboBox();
        cmbFiltroForma.SetBounds(656, 8, 190, 30);
        cmbFiltroForma.SelectedIndexChanged += (_, _) => CarregarGrid();

        var btnBuscar = UITheme.CriarBotao("🔍 Buscar", null, true);
        btnBuscar.SetBounds(858, 8, 100, 30);
        btnBuscar.Click += (_, _) => CarregarGrid();

        var btnLimpar = UITheme.CriarBotao("✖ Limpar", UITheme.TextMuted, true);
        btnLimpar.SetBounds(968, 8, 100, 30);
        btnLimpar.Click += (_, _) =>
        {
            txtBusca.Clear();
            cmbFiltroCliente.SelectedIndex = 0;
            cmbFiltroStatus.SelectedIndex = 0;
            cmbFiltroForma.SelectedIndex = 0;
            CarregarGrid();
        };

        var btnNovo = UITheme.CriarBotao("➕ Registrar Pagamento");
        btnNovo.SetBounds(0, 50, 180, 36);
        btnNovo.Click += (_, _) => AbrirFormulario(null);

        var btnEditar = UITheme.CriarBotao("✏ Editar", UITheme.Warning);
        btnEditar.SetBounds(190, 50, 100, 36);
        btnEditar.Click += (_, _) => EditarSelecionado();

        var btnExcluir = UITheme.CriarBotao("🗑 Excluir", UITheme.Danger);
        btnExcluir.SetBounds(300, 50, 100, 36);
        btnExcluir.Click += (_, _) => ExcluirSelecionado();

        lblResumo = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            AutoSize = false,
            Bounds = new Rectangle(420, 50, 650, 36),
            TextAlign = ContentAlignment.MiddleRight
        };

        filtros.Controls.AddRange(new Control[]
        {
            txtBusca, cmbFiltroCliente, cmbFiltroStatus, cmbFiltroForma,
            btnBuscar, btnLimpar, btnNovo, btnEditar, btnExcluir, lblResumo
        });

        // ---------- Grid ----------
        grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 28 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "Valor", FillWeight = 65 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vencimento", HeaderText = "Vencimento", FillWeight = 62 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pagamento", HeaderText = "Pagamento", FillWeight = 62 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Forma", HeaderText = "Forma de Pagamento", FillWeight = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 55 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Observacao", HeaderText = "Observação", FillWeight = 110 });
        grid.CellDoubleClick += (_, _) => EditarSelecionado();
        grid.CellFormatting += Grid_CellFormatting;

        var cardGrid = UITheme.CriarCard();
        cardGrid.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(filtros);
        Controls.Add(lblTitulo);
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null) return;
        if (grid.Rows[e.RowIndex].Cells["Status"].Value is not string status) return;
        e.CellStyle.ForeColor = status switch
        {
            "Pago" => UITheme.Success,
            "Pendente" => UITheme.Warning,
            "Atrasado" => UITheme.Danger,
            _ => e.CellStyle.ForeColor
        };
        if (grid.Columns[e.ColumnIndex].Name == "Status")
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
    }

    private void Inicializar()
    {
        try
        {
            var db = new DatabaseConnection(Program.ConnectionString);
            _formaRepo = new FormaPagamentoRepository(db);
            var clienteRepo = new ClienteRepository(db);
            _clienteService = new ClienteService(clienteRepo);
            _service = new PagamentoService(new PagamentoRepository(db), clienteRepo, _formaRepo);

            // Combo de filtro de clientes
            cmbFiltroCliente.Items.Clear();
            cmbFiltroCliente.Items.Add("(Todos os clientes)");
            foreach (var c in _clienteService.ListarAtivos()) cmbFiltroCliente.Items.Add(c);
            cmbFiltroCliente.SelectedIndex = 0;

            // Combo de filtro de formas (inclui inativas para não esconder histórico)
            cmbFiltroForma.Items.Clear();
            cmbFiltroForma.Items.Add("(Todas as formas)");
            foreach (var f in _formaRepo.Listar()) cmbFiltroForma.Items.Add(f);
            cmbFiltroForma.SelectedIndex = 0;

            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar os pagamentos.", ex, this);
        }
    }

    private void CarregarGrid()
    {
        if (_service is null) return;
        try
        {
            int? clienteId = cmbFiltroCliente.SelectedItem is Cliente c ? c.Id : null;
            string? status = cmbFiltroStatus.SelectedIndex > 0 ? cmbFiltroStatus.SelectedItem?.ToString() : null;
            int? formaId = cmbFiltroForma.SelectedItem is FormaPagamento f ? f.Id : null;

            var pagamentos = _service.Listar(txtBusca.Text.Trim(), clienteId, status, formaId);

            grid.Rows.Clear();
            foreach (var p in pagamentos)
            {
                grid.Rows.Add(p.Id, p.ClienteNome, p.Valor.ToString("C2"),
                    p.DataVencimento.ToString("dd/MM/yyyy"),
                    p.DataPagamento?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    p.FormaPagamentoNome, p.Status, p.Observacao ?? "");
            }

            var resumo = _service.ObterResumo();
            lblResumo.Text = $"Recebido: {resumo.ValorRecebido:C2}   |   Pendente: {resumo.ValorPendente:C2}   |   " +
                             $"Pagos: {resumo.Pagos} · Pendentes: {resumo.Pendentes} · Atrasados: {resumo.Atrasados}";
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao carregar a lista de pagamentos.", ex, this);
        }
    }

    private Pagamento? Selecionado()
    {
        if (grid.SelectedRows.Count == 0) return null;
        var id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
        return _service.ObterPorId(id);
    }

    private void EditarSelecionado()
    {
        var pagamento = Selecionado();
        if (pagamento is null)
        {
            ErrorHelper.ShowWarning("Selecione um pagamento na lista.", this);
            return;
        }
        AbrirFormulario(pagamento);
    }

    private void AbrirFormulario(Pagamento? pagamento)
    {
        var clientes = _clienteService.ListarAtivos();
        // Inclui formas ativas + a forma atual do pagamento (mesmo que desativada)
        var formas = _formaRepo.Listar(true);
        if (pagamento is not null && formas.All(f => f.Id != pagamento.FormaPagamentoId))
        {
            var atual = _formaRepo.ObterPorId(pagamento.FormaPagamentoId);
            if (atual is not null) formas.Add(atual);
        }

        using var form = new PagamentoForm(_service, _usuario, clientes, formas, pagamento);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            InicializarFiltros();
            CarregarGrid();
        }
    }

    private void InicializarFiltros()
    {
        var selCliente = cmbFiltroCliente.SelectedItem;
        cmbFiltroCliente.Items.Clear();
        cmbFiltroCliente.Items.Add("(Todos os clientes)");
        foreach (var c in _clienteService.ListarAtivos()) cmbFiltroCliente.Items.Add(c);
        cmbFiltroCliente.SelectedIndex = selCliente is Cliente sc &&
            cmbFiltroCliente.Items.Cast<object>().OfType<Cliente>().Any(x => x.Id == sc.Id)
            ? cmbFiltroCliente.Items.IndexOf(cmbFiltroCliente.Items.Cast<object>().OfType<Cliente>().First(x => x.Id == sc.Id))
            : 0;
    }

    private void ExcluirSelecionado()
    {
        var pagamento = Selecionado();
        if (pagamento is null)
        {
            ErrorHelper.ShowWarning("Selecione um pagamento na lista.", this);
            return;
        }
        if (MessageBox.Show(this,
                $"Excluir o pagamento #{pagamento.Id} de {pagamento.ClienteNome} ({pagamento.Valor:C2})?\n(Exclusão lógica: o registro é preservado no banco.)",
                "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            _service.Excluir(pagamento.Id);
            ErrorHelper.ShowSuccess("Pagamento excluído com sucesso.", this);
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao excluir o pagamento.", ex, this);
        }
    }
}
