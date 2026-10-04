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
    private Button btnNovo = null!, btnEditar = null!, btnDesativar = null!, btnBuscar = null!;

    private ClienteService _service = null!;

    public ClientesView()
    {
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
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UITheme.Background };

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

        toolbar.Controls.Add(txtBusca);
        toolbar.Controls.Add(btnBuscar);
        toolbar.Controls.Add(btnNovo);
        toolbar.Controls.Add(btnEditar);
        toolbar.Controls.Add(btnDesativar);
        toolbar.Resize += (_, _) => PosicionarToolbar(toolbar);
        PosicionarToolbar(toolbar);

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

        var cardGrid = UITheme.CriarCard();
        cardGrid.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(toolbar);
        Controls.Add(lblTitulo);
    }

    private void PosicionarToolbar(Panel toolbar)
    {
        int x = 0;
        foreach (Control c in new Control[] { txtBusca, btnBuscar, btnNovo, btnEditar, btnDesativar })
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
            _service = new ClienteService(new ClienteRepository(db));
            CarregarGrid();
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
