using System.Globalization;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>Formulário de registro/edição de pagamento (modal).</summary>
public class PagamentoForm : Form
{
    private readonly PagamentoService _service;
    private readonly Usuario _usuario;
    private readonly Pagamento? _pagamento;

    private ComboBox cmbCliente = null!, cmbForma = null!, cmbStatus = null!;
    private TextBox txtValor = null!, txtObservacao = null!;
    private DateTimePicker dtpVencimento = null!, dtpPagamento = null!;
    private CheckBox chkPago = null!;
    private Label lblResumoCliente = null!;

    public PagamentoForm(PagamentoService service, Usuario usuario,
        List<Cliente> clientes, List<FormaPagamento> formas, Pagamento? pagamento)
    {
        _service = service;
        _usuario = usuario;
        _pagamento = pagamento;

        ConstruirInterface(clientes, formas);
        if (pagamento is not null) Preencher(pagamento);
    }

    private void ConstruirInterface(List<Cliente> clientes, List<FormaPagamento> formas)
    {
        Text = _pagamento is null ? "Registrar Pagamento" : "Editar Pagamento";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;

        var card = UITheme.CriarCard();
        card.SetBounds(20, 20, 440, 580);

        int x = 24, w = 392, y = 20;

        void AddCampo(string rotulo, Control controle, int altura = 30)
        {
            var lbl = UITheme.CriarLabel(rotulo);
            lbl.Location = new Point(x, y);
            lbl.Font = UITheme.FontPequena;
            card.Controls.Add(lbl);
            y += 20;
            controle.SetBounds(x, y, w, altura);
            card.Controls.Add(controle);
            y += altura + 10;
        }

        var lblTitulo = UITheme.CriarLabel(
            _pagamento is null ? "➕ Registrar Pagamento" : "✏ Editar Pagamento", true, UITheme.Primary);
        lblTitulo.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
        lblTitulo.Location = new Point(x, y);
        card.Controls.Add(lblTitulo);
        y += 36;

        // Cliente
        cmbCliente = UITheme.CriarComboBox();
        cmbCliente.DataSource = clientes;
        cmbCliente.SelectedIndexChanged += (_, _) => AtualizarResumoCliente();
        AddCampo("Cliente *", cmbCliente);

        lblResumoCliente = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            AutoSize = false,
            Bounds = new Rectangle(x, y, w, 34)
        };
        card.Controls.Add(lblResumoCliente);
        y += 42;

        // Valor
        txtValor = UITheme.CriarTextBox("0,00");
        txtValor.TextAlign = HorizontalAlignment.Right;
        AddCampo("Valor (R$) *", txtValor);

        // Vencimento
        dtpVencimento = new DateTimePicker { Font = UITheme.FontNormal, Format = DateTimePickerFormat.Short };
        AddCampo("Data de vencimento *", dtpVencimento);

        // Status
        cmbStatus = UITheme.CriarComboBox();
        cmbStatus.Items.AddRange(new object[] { "Pendente", "Pago", "Atrasado" });
        cmbStatus.SelectedIndex = 0;
        cmbStatus.SelectedIndexChanged += (_, _) => AtualizarDataPagamento();
        AddCampo("Status *", cmbStatus);

        // Pago + data
        chkPago = new CheckBox
        {
            Text = "Foi pago (informar data do pagamento)",
            Font = UITheme.FontNormal,
            ForeColor = UITheme.Text,
            AutoSize = true,
            Location = new Point(x, y)
        };
        chkPago.CheckedChanged += (_, _) =>
        {
            dtpPagamento.Enabled = chkPago.Checked;
            if (chkPago.Checked) cmbStatus.SelectedItem = "Pago";
        };
        card.Controls.Add(chkPago);
        y += 28;

        dtpPagamento = new DateTimePicker
        {
            Font = UITheme.FontNormal,
            Format = DateTimePickerFormat.Short,
            Enabled = false
        };
        AddCampo("Data do pagamento", dtpPagamento);

        // Forma de pagamento
        cmbForma = UITheme.CriarComboBox();
        cmbForma.DataSource = formas;
        AddCampo("Forma de pagamento *", cmbForma);

        // Observação
        txtObservacao = UITheme.CriarTextBox("Observações (opcional)");
        txtObservacao.Multiline = true;
        txtObservacao.ScrollBars = ScrollBars.Vertical;
        AddCampo("Observação", txtObservacao, 60);

        var btnSalvar = UITheme.CriarBotao("💾 Salvar");
        btnSalvar.SetBounds(x, 520, 190, 40);
        btnSalvar.Click += BtnSalvar_Click;

        var btnCancelar = UITheme.CriarBotao("Cancelar", UITheme.TextMuted, true);
        btnCancelar.SetBounds(x + 202, 520, 190, 40);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        card.Controls.Add(btnSalvar);
        card.Controls.Add(btnCancelar);
        Controls.Add(card);

        AtualizarDataPagamento();
        AtualizarResumoCliente();
    }

    private void AtualizarDataPagamento()
    {
        bool pago = cmbStatus.SelectedItem?.ToString() == "Pago";
        if (pago && !chkPago.Checked) chkPago.Checked = true;
        dtpPagamento.Enabled = pago;
    }

    private void AtualizarResumoCliente()
    {
        if (cmbCliente.SelectedItem is not Cliente cliente)
        {
            lblResumoCliente.Text = "";
            return;
        }
        try
        {
            var r = _service.ObterResumoCliente(cliente.Id);
            lblResumoCliente.Text = r.QuantidadeParcelas == 0
                ? $"Cliente sem pagamentos registrados."
                : $"Total: {r.ValorTotal:C2} · Parcelas: {r.ParcelasPagas}/{r.QuantidadeParcelas} pagas · " +
                  $"Pago: {r.TotalPago:C2} · Restante: {r.ValorRestante:C2} · {r.Status}";
        }
        catch
        {
            lblResumoCliente.Text = "";
        }
    }

    private void Preencher(Pagamento p)
    {
        for (int i = 0; i < cmbCliente.Items.Count; i++)
            if (cmbCliente.Items[i] is Cliente c && c.Id == p.ClienteId) { cmbCliente.SelectedIndex = i; break; }

        txtValor.Text = p.Valor.ToString("F2", CultureInfo.GetCultureInfo("pt-BR"));
        dtpVencimento.Value = p.DataVencimento;
        cmbStatus.SelectedItem = p.Status;
        txtObservacao.Text = p.Observacao ?? "";

        if (p.DataPagamento.HasValue)
        {
            chkPago.Checked = true;
            dtpPagamento.Value = p.DataPagamento.Value;
        }

        for (int i = 0; i < cmbForma.Items.Count; i++)
            if (cmbForma.Items[i] is FormaPagamento f && f.Id == p.FormaPagamentoId) { cmbForma.SelectedIndex = i; break; }
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        try
        {
            if (cmbCliente.SelectedItem is not Cliente cliente)
            {
                ErrorHelper.ShowWarning("Selecione um cliente.", this);
                return;
            }
            if (cmbForma.SelectedItem is not FormaPagamento forma)
            {
                ErrorHelper.ShowWarning("Selecione uma forma de pagamento.", this);
                return;
            }
            if (!decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(),
                    NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out var valor))
            {
                ErrorHelper.ShowWarning("Valor inválido. Exemplo: 150,00", this);
                return;
            }

            var pagamento = _pagamento ?? new Pagamento();
            pagamento.ClienteId = cliente.Id;
            pagamento.Valor = valor;
            pagamento.DataVencimento = dtpVencimento.Value.Date;
            pagamento.Status = cmbStatus.SelectedItem?.ToString() ?? "Pendente";
            pagamento.FormaPagamentoId = forma.Id;
            pagamento.Observacao = string.IsNullOrWhiteSpace(txtObservacao.Text) ? null : txtObservacao.Text.Trim();
            pagamento.UsuarioId = _usuario.Id;
            pagamento.DataPagamento = chkPago.Checked && dtpPagamento.Enabled
                ? dtpPagamento.Value
                : pagamento.Status == "Pago" ? DateTime.Now : null;

            if (_pagamento is null)
            {
                _service.Registrar(pagamento);
                ErrorHelper.ShowSuccess("Pagamento registrado com sucesso.", this);
            }
            else
            {
                _service.Atualizar(pagamento);
                ErrorHelper.ShowSuccess("Pagamento atualizado com sucesso.", this);
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
                _pagamento is null ? "Erro ao registrar o pagamento." : "Erro ao atualizar o pagamento.",
                ex, this);
        }
    }
}
