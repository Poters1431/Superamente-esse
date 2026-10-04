using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>Formulário de cadastro/edição de cliente (modal).</summary>
public class ClienteForm : Form
{
    private readonly ClienteService _service;
    private readonly Cliente? _cliente;

    private TextBox txtNome = null!, txtCpf = null!, txtTelefone = null!, txtEmail = null!, txtEndereco = null!;
    private DateTimePicker dtpNascimento = null!;
    private CheckBox chkSemNascimento = null!;
    private Button btnSalvar = null!, btnCancelar = null!;

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
        ClientSize = new Size(460, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;

        var card = UITheme.CriarCard();
        card.SetBounds(20, 20, 420, 480);

        int x = 24, w = 372, y = 20;

        void AddCampo(string rotulo, Control controle, int altura = 28)
        {
            var lbl = UITheme.CriarLabel(rotulo);
            lbl.Location = new Point(x, y);
            card.Controls.Add(lbl);
            y += 22;
            controle.SetBounds(x, y, w, altura);
            card.Controls.Add(controle);
            y += altura + 14;
        }

        var lblTitulo = UITheme.CriarLabel(_cliente is null ? "➕ Novo Cliente" : "✏ Editar Cliente", true, UITheme.Primary);
        lblTitulo.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
        lblTitulo.Location = new Point(x, y);
        card.Controls.Add(lblTitulo);
        y += 40;

        txtNome = UITheme.CriarTextBox("Nome completo");
        AddCampo("Nome *", txtNome);

        txtCpf = UITheme.CriarTextBox("Somente números");
        AddCampo("CPF *", txtCpf);

        txtTelefone = UITheme.CriarTextBox("(00) 00000-0000");
        AddCampo("Telefone", txtTelefone);

        txtEmail = UITheme.CriarTextBox("email@exemplo.com");
        AddCampo("E-mail", txtEmail);

        dtpNascimento = new DateTimePicker
        {
            Font = UITheme.FontNormal,
            Format = DateTimePickerFormat.Short,
            MinDate = new DateTime(1900, 1, 1),
            MaxDate = DateTime.Today
        };
        AddCampo("Data de nascimento", dtpNascimento);

        chkSemNascimento = new CheckBox
        {
            Text = "Não informar data de nascimento",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            AutoSize = true,
            Location = new Point(x, y)
        };
        chkSemNascimento.CheckedChanged += (_, _) => dtpNascimento.Enabled = !chkSemNascimento.Checked;
        card.Controls.Add(chkSemNascimento);
        y += 30;

        txtEndereco = UITheme.CriarTextBox("Rua, número, bairro, cidade...");
        AddCampo("Endereço", txtEndereco, 40);

        btnSalvar = UITheme.CriarBotao("💾 Salvar");
        btnSalvar.SetBounds(x, 420, 180, 40);
        btnSalvar.Click += BtnSalvar_Click;

        btnCancelar = UITheme.CriarBotao("Cancelar", UITheme.TextMuted, true);
        btnCancelar.SetBounds(x + 192, 420, 180, 40);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        card.Controls.Add(btnSalvar);
        card.Controls.Add(btnCancelar);
        Controls.Add(card);
    }

    private void Preencher(Cliente c)
    {
        txtNome.Text = c.NomeCompleto;
        txtCpf.Text = c.CPF;
        txtTelefone.Text = c.Telefone ?? "";
        txtEmail.Text = c.Email ?? "";
        txtEndereco.Text = c.Endereco ?? "";
        if (c.DataNascimento.HasValue) dtpNascimento.Value = c.DataNascimento.Value;
        else chkSemNascimento.Checked = true;
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            var cliente = _cliente ?? new Cliente();
            cliente.NomeCompleto = txtNome.Text.Trim();
            cliente.CPF = txtCpf.Text.Trim();
            cliente.Telefone = string.IsNullOrWhiteSpace(txtTelefone.Text) ? null : txtTelefone.Text.Trim();
            cliente.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            cliente.Endereco = string.IsNullOrWhiteSpace(txtEndereco.Text) ? null : txtEndereco.Text.Trim();
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
