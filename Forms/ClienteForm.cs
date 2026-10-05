using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>Formulário de cadastro/edição de cliente (modal).</summary>
public class ClienteForm : Form
{
    private readonly ClienteService _service;
    private readonly CepService _cepService = new();
    private readonly Cliente? _cliente;

    private TextBox txtNome = null!, txtCpf = null!, txtTelefone = null!, txtEmail = null!;
    private TextBox txtCep = null!, txtRua = null!, txtComplemento = null!;
    private DateTimePicker dtpNascimento = null!;
    private CheckBox chkSemNascimento = null!;
    private Label lblCepStatus = null!;
    private Button btnSalvar = null!, btnCancelar = null!;
    private bool _buscandoCep;

    public ClienteForm(ClienteService service, Cliente? cliente)
    {
        _service = service;
        _cliente = cliente;
        ConstruirInterface();
        if (cliente is not null) Preencher(cliente);
    }

    private void ConstruirInterface()
    {
        Text = _cliente is null ? "Novo Cliente" : "Editar Cliente";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 700);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;
        Padding = new Padding(16);

        var card = UITheme.CriarCard();
        card.Dock = DockStyle.Fill;

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        Label Rotulo(string texto) => new()
        {
            Text = texto,
            Font = UITheme.FontPequena,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        };

        void Linha(Control rotulo, Control campo, float alturaCampo = 30f)
        {
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            tabela.Controls.Add(rotulo);
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, alturaCampo));
            tabela.Controls.Add(campo);
        }

        // Título
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
        tabela.Controls.Add(new Label
        {
            Text = _cliente is null ? "➕ Novo Cliente" : "✏ Editar Cliente",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = UITheme.Primary,
            Dock = DockStyle.Fill
        });

        txtNome = UITheme.CriarTextBox("Nome completo");
        txtCpf = UITheme.CriarTextBox("Somente números");
        txtTelefone = UITheme.CriarTextBox("(00) 00000-0000");
        txtEmail = UITheme.CriarTextBox("email@exemplo.com");

        txtCep = UITheme.CriarTextBox("Somente números — preenche a rua automaticamente");
        txtCep.Leave += async (_, _) => await BuscarCepAsync();

        txtRua = UITheme.CriarTextBox("Rua / Logradouro (preenchido pelo CEP)");
        txtComplemento = UITheme.CriarTextBox("Apto, bloco, casa... (opcional)");

        foreach (var t in new[] { txtNome, txtCpf, txtTelefone, txtEmail, txtCep, txtRua, txtComplemento })
            t.Dock = DockStyle.Fill;

        dtpNascimento = new DateTimePicker
        {
            Font = UITheme.FontNormal,
            Format = DateTimePickerFormat.Short,
            MinDate = new DateTime(1900, 1, 1),
            MaxDate = DateTime.Today,
            Dock = DockStyle.Fill
        };

        chkSemNascimento = new CheckBox
        {
            Text = "Não informar data de nascimento",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Dock = DockStyle.Fill
        };
        chkSemNascimento.CheckedChanged += (_, _) => dtpNascimento.Enabled = !chkSemNascimento.Checked;

        lblCepStatus = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Dock = DockStyle.Fill
        };

        Linha(Rotulo("Nome *"), txtNome);
        Linha(Rotulo("CPF *"), txtCpf);
        Linha(Rotulo("Telefone"), txtTelefone);
        Linha(Rotulo("E-mail"), txtEmail);
        Linha(Rotulo("Data de nascimento"), dtpNascimento);

        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        tabela.Controls.Add(chkSemNascimento);

        Linha(Rotulo("CEP (busca a rua sozinho)"), txtCep);

        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
        tabela.Controls.Add(lblCepStatus);

        Linha(Rotulo("Rua"), txtRua);
        Linha(Rotulo("Complemento (opcional)"), txtComplemento);

        // Espaço flexível + linha de botões (sempre visível)
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        tabela.Controls.Add(new Panel());

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        btnSalvar = UITheme.CriarBotao("💾 Salvar");
        btnSalvar.Width = 150;
        btnSalvar.Click += BtnSalvar_Click;
        btnCancelar = UITheme.CriarBotao("Cancelar", UITheme.TextMuted, true);
        btnCancelar.Width = 130;
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        painelBotoes.Controls.Add(btnSalvar);
        painelBotoes.Controls.Add(btnCancelar);

        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
        tabela.Controls.Add(painelBotoes);

        tabela.RowCount = tabela.RowStyles.Count;
        card.Controls.Add(tabela);
        Controls.Add(card);

        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;
    }

    private async Task BuscarCepAsync()
    {
        var cep = ValidationHelper.SomenteNumeros(txtCep.Text);
        if (cep.Length != 8 || _buscandoCep) return;

        _buscandoCep = true;
        lblCepStatus.ForeColor = UITheme.TextMuted;
        lblCepStatus.Text = "🔎 Buscando endereço pelo CEP...";
        try
        {
            var endereco = await _cepService.BuscarAsync(cep);
            if (endereco is null)
            {
                lblCepStatus.ForeColor = UITheme.Danger;
                lblCepStatus.Text = "⚠ CEP não encontrado. Digite a rua manualmente.";
                return;
            }

            var partes = new[] { endereco.Logradouro, endereco.Bairro, endereco.Localidade,
                                 endereco.Uf }.Where(p => !string.IsNullOrWhiteSpace(p));
            txtRua.Text = string.Join(", ", partes);
            if (!string.IsNullOrWhiteSpace(endereco.Complemento) && string.IsNullOrWhiteSpace(txtComplemento.Text))
                txtComplemento.Text = endereco.Complemento;

            lblCepStatus.ForeColor = UITheme.Success;
            lblCepStatus.Text = "✅ Endereço encontrado. Confira o número/complemento.";
        }
        catch (Exception ex)
        {
            // Sem internet ou API indisponível: não bloqueia o cadastro.
            lblCepStatus.ForeColor = UITheme.Warning;
            lblCepStatus.Text = "⚠ Não foi possível consultar o CEP. Digite a rua manualmente.";
            System.Diagnostics.Debug.WriteLine("Erro ViaCEP: " + ex);
        }
        finally
        {
            _buscandoCep = false;
        }
    }

    private void Preencher(Cliente c)
    {
        txtNome.Text = c.NomeCompleto;
        txtCpf.Text = c.CPF;
        txtTelefone.Text = c.Telefone ?? "";
        txtEmail.Text = c.Email ?? "";
        txtCep.Text = c.CEP ?? "";
        txtRua.Text = c.Endereco ?? "";
        txtComplemento.Text = c.Complemento ?? "";
        if (c.DataNascimento.HasValue) dtpNascimento.Value = c.DataNascimento.Value;
        else chkSemNascimento.Checked = true;
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            var cepDigitos = ValidationHelper.SomenteNumeros(txtCep.Text);
            if (!string.IsNullOrEmpty(cepDigitos) && cepDigitos.Length != 8)
            {
                ErrorHelper.ShowWarning("CEP inválido. Digite os 8 números ou deixe em branco.", this);
                return;
            }

            var cliente = _cliente ?? new Cliente();
            cliente.NomeCompleto = txtNome.Text.Trim();
            cliente.CPF = txtCpf.Text.Trim();
            cliente.Telefone = string.IsNullOrWhiteSpace(txtTelefone.Text) ? null : txtTelefone.Text.Trim();
            cliente.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            cliente.CEP = string.IsNullOrEmpty(cepDigitos) ? null : cepDigitos;
            cliente.Endereco = string.IsNullOrWhiteSpace(txtRua.Text) ? null : txtRua.Text.Trim();
            cliente.Complemento = string.IsNullOrWhiteSpace(txtComplemento.Text) ? null : txtComplemento.Text.Trim();
            cliente.DataNascimento = chkSemNascimento.Checked ? null : dtpNascimento.Value.Date;

            if (_cliente is null)
            {
                _service.Cadastrar(cliente);
                ErrorHelper.ShowSuccess("Cliente cadastrado com sucesso.", this);
            }
            else
            {
                _service.Atualizar(cliente);
                ErrorHelper.ShowSuccess("Cliente atualizado com sucesso.", this);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            ErrorHelper.ShowWarning(ex.Message, this);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError(
                _cliente is null ? "Erro ao cadastrar o cliente." : "Erro ao atualizar o cliente.",
                ex, this);
        }
    }
}
