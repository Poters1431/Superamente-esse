using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;

namespace ClientePagamentoSystem.Forms;

public class FormasPagamentoView : UserControl
{
    private DataGridView grid = null!;
    private TextBox txtNome = null!, txtDescricao = null!;
    private FormaPagamentoRepository _repo = null!;
    private FormaPagamento? _selecionada;

    public FormasPagamentoView()
    {
        ConstruirInterface();
        Load += (_, _) => Inicializar();
    }

    private void ConstruirInterface()
    {
        BackColor = UITheme.Background;

        var lblTitulo = new Label
        {
            Text = "💳 Formas de Pagamento",
            Font = UITheme.FontTitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 50
        };

        // Formulário lateral
        var painelForm = UITheme.CriarCard();
        painelForm.Dock = DockStyle.Right;
        painelForm.Width = 330;

        var lblForm = UITheme.CriarLabel("Nova / Editar forma", true, UITheme.Primary);
        lblForm.Location = new Point(16, 16);

        painelForm.Controls.Add(lblForm);
        painelForm.Controls.Add(UITheme.CriarLabel("Nome *"));
        painelForm.Controls[^1].Location = new Point(16, 60);

        txtNome = UITheme.CriarTextBox("Ex.: Vale-presente");
        txtNome.SetBounds(16, 82, 290, 30);
        painelForm.Controls.Add(txtNome);

        painelForm.Controls.Add(UITheme.CriarLabel("Descrição"));
        painelForm.Controls[^1].Location = new Point(16, 124);

        txtDescricao = UITheme.CriarTextBox("Descrição (opcional)");
        txtDescricao.Multiline = true;
        txtDescricao.SetBounds(16, 146, 290, 60);
        painelForm.Controls.Add(txtDescricao);

        var btnSalvar = UITheme.CriarBotao("💾 Salvar");
        btnSalvar.SetBounds(16, 226, 140, 38);
        btnSalvar.Click += BtnSalvar_Click;

        var btnLimpar = UITheme.CriarBotao("Limpar", UITheme.TextMuted, true);
        btnLimpar.SetBounds(166, 226, 140, 38);
        btnLimpar.Click += (_, _) => LimparForm();

        var btnDesativar = UITheme.CriarBotao("🗑 Desativar", UITheme.Danger);
        btnDesativar.SetBounds(16, 274, 140, 38);
        btnDesativar.Click += BtnDesativar_Click;

        var btnReativar = UITheme.CriarBotao("♻ Reativar", UITheme.Success);
        btnReativar.SetBounds(166, 274, 140, 38);
        btnReativar.Click += BtnReativar_Click;

        var lblInfo = new Label
        {
            Text = "ℹ Desativar não apaga os pagamentos históricos que usam esta forma.",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            AutoSize = false,
            Bounds = new Rectangle(16, 330, 290, 60)
        };

        painelForm.Controls.AddRange(new Control[] { btnSalvar, btnLimpar, btnDesativar, btnReativar, lblInfo });

        // Grid
        grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 25 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome", FillWeight = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descricao", HeaderText = "Descrição", FillWeight = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 40 });
        grid.SelectionChanged += (_, _) => SelecionarLinha();

        var cardGrid = UITheme.CriarCard();
        cardGrid.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(painelForm);
        Controls.Add(lblTitulo);
    }

    private void Inicializar()
    {
        try
        {
            _repo = new FormaPagamentoRepository(new DatabaseConnection(Program.ConnectionString));
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar as formas de pagamento.", ex, this);
        }
    }

    private void CarregarGrid()
    {
        try
        {
            grid.Rows.Clear();
            foreach (var f in _repo.Listar())
                grid.Rows.Add(f.Id, f.Nome, f.Descricao ?? "", f.Ativo ? "Ativa" : "Inativa");
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao listar as formas de pagamento.", ex, this);
        }
    }

    private void SelecionarLinha()
    {
        if (grid.SelectedRows.Count == 0) { _selecionada = null; return; }
        var id = Convert.ToInt32(grid.SelectedRows[0].Cells["Id"].Value);
        try
        {
            _selecionada = _repo.ObterPorId(id);
            if (_selecionada is not null)
            {
                txtNome.Text = _selecionada.Nome;
                txtDescricao.Text = _selecionada.Descricao ?? "";
            }
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao carregar a forma selecionada.", ex, this);
        }
    }

    private void LimparForm()
    {
        _selecionada = null;
        txtNome.Clear();
        txtDescricao.Clear();
        grid.ClearSelection();
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                ErrorHelper.ShowWarning("Informe o nome da forma de pagamento.", this);
                return;
            }

            var forma = _selecionada ?? new FormaPagamento();
            forma.Nome = txtNome.Text.Trim();
            forma.Descricao = string.IsNullOrWhiteSpace(txtDescricao.Text) ? null : txtDescricao.Text.Trim();

            if (_selecionada is null)
            {
                _repo.Inserir(forma);
                ErrorHelper.ShowSuccess("Forma de pagamento cadastrada.", this);
            }
            else
            {
                _repo.Atualizar(forma);
                ErrorHelper.ShowSuccess("Forma de pagamento atualizada.", this);
            }
            LimparForm();
            CarregarGrid();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            ErrorHelper.ShowWarning("Já existe uma forma de pagamento com este nome.", this);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao salvar a forma de pagamento.", ex, this);
        }
    }

    private void BtnDesativar_Click(object? sender, EventArgs e)
    {
        if (_selecionada is null)
        {
            ErrorHelper.ShowWarning("Selecione uma forma de pagamento na lista.", this);
            return;
        }
        if (MessageBox.Show(this, $"Desativar '{_selecionada.Nome}'?\nOs pagamentos históricos serão mantidos.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        try
        {
            _repo.Desativar(_selecionada.Id);
            ErrorHelper.ShowSuccess("Forma de pagamento desativada.", this);
            LimparForm();
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao desativar a forma de pagamento.", ex, this);
        }
    }

    private void BtnReativar_Click(object? sender, EventArgs e)
    {
        if (_selecionada is null)
        {
            ErrorHelper.ShowWarning("Selecione uma forma de pagamento na lista.", this);
            return;
        }
        try
        {
            _repo.Reativar(_selecionada.Id);
            ErrorHelper.ShowSuccess("Forma de pagamento reativada.", this);
            LimparForm();
            CarregarGrid();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Erro ao reativar a forma de pagamento.", ex, this);
        }
    }
}
