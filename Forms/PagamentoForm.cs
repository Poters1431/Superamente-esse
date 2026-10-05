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
    private Button btnSalvar = null!, btnCancelar = null!;

    public PagamentoForm(PagamentoService service, Usuario usuario,
        List<Cliente> clientes, List<FormaPagamento> formas, Pagamento? pagamento,
        int? clientePreselecionadoId = null, decimal? valorSugerido = null,
        string? observacaoSugerida = null, bool jaPago = false)
    {
        _service = service;
        _usuario = usuario;
        _pagamento = pagamento;

        ConstruirInterface(clientes, formas);
        if (pagamento is not null) Preencher(pagamento);
        else
        {
            if (clientePreselecionadoId.HasValue)
                for (int i = 0; i < cmbCliente.Items.Count; i++)
                    if (cmbCliente.Items[i] is Cliente c && c.Id == clientePreselecionadoId.Value)
                    { cmbCliente.SelectedIndex = i; break; }
            if (valorSugerido.HasValue)
                txtValor.Text = valorSugerido.Value.ToString("F2", CultureInfo.GetCultureInfo("pt-BR"));
            if (!string.IsNullOrWhiteSpace(observacaoSugerida))
                txtObservacao.Text = observacaoSugerida;
            if (jaPago)
            {
                cmbStatus.SelectedItem = "Pago";
                chkPago.Checked = true;
                dtpPagamento.Value = DateTime.Today;
            }
        }
    }

    private void ConstruirInterface(List<Cliente> clientes, List<FormaPagamento> formas)
    {
        Text = _pagamento is null ? "Registrar Pagamento" : "Editar Pagamento";
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
            Text = _pagamento is null ? "➕ Registrar Pagamento" : "✏ Editar Pagamento",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = UITheme.Primary,
            Dock = DockStyle.Fill
        });

        // Cliente
        cmbCliente = UITheme.CriarComboBox();
        cmbCliente.DataSource = clientes;
        cmbCliente.Dock = DockStyle.Fill;
        cmbCliente.SelectedIndexChanged += (_, _) => AtualizarResumoCliente();
        Linha(Rotulo("Cliente *"), cmbCliente);

        // Resumo financeiro do cliente
        lblResumoCliente = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Dock = DockStyle.Fill
        };
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        tabela.Controls.Add(lblResumoCliente);

        // Valor
        txtValor = UITheme.CriarTextBox("0,00");
        txtValor.TextAlign = HorizontalAlignment.Right;
        txtValor.Dock = DockStyle.Fill;
        Linha(Rotulo("Valor (R$) *"), txtValor);

        // Vencimento
        dtpVencimento = new DateTimePicker
        {
            Font = UITheme.FontNormal,
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Fill
        };
        Linha(Rotulo("Data de vencimento *"), dtpVencimento);

        // Status
        cmbStatus = UITheme.CriarComboBox();
        cmbStatus.Items.AddRange(new object[] { "Pendente", "Pago", "Atrasado" });
        cmbStatus.SelectedIndex = 0;
        cmbStatus.Dock = DockStyle.Fill;
        cmbStatus.SelectedIndexChanged += (_, _) => AtualizarDataPagamento();
        Linha(Rotulo("Status *"), cmbStatus);

        // Checkbox pago
        chkPago = new CheckBox
        {
            Text = "Foi pago (informar data do pagamento)",
            Font = UITheme.FontPequena,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Fill
        };
        chkPago.CheckedChanged += (_, _) =>
        {
            dtpPagamento.Enabled = chkPago.Checked;
            if (chkPago.Checked) cmbStatus.SelectedItem = "Pago";
        };
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        tabela.Controls.Add(chkPago);

        // Data do pagamento
        dtpPagamento = new DateTimePicker
        {
            Font = UITheme.FontNormal,
            Format = DateTimePickerFormat.Short,
            Enabled = false,
            Dock = DockStyle.Fill
        };
        Linha(Rotulo("Data do pagamento"), dtpPagamento);

        // Forma de pagamento
        cmbForma = UITheme.CriarComboBox();
        cmbForma.DataSource = formas;
        cmbForma.Dock = DockStyle.Fill;
        Linha(Rotulo("Forma de pagamento *"), cmbForma);

        // Observação
        txtObservacao = UITheme.CriarTextBox("Observações (opcional)");
        txtObservacao.Multiline = true;
        txtObservacao.ScrollBars = ScrollBars.Vertical;
        txtObservacao.Dock = DockStyle.Fill;
        Linha(Rotulo("Observação"), txtObservacao, 56f);

        // Espaço flexível + botões sempre visíveis
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        tabela.Controls.Add(new Panel());

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        btnSalvar = UITheme.CriarBotao("💾 Salvar");
        btnSalvar.Width = 160;
        btnSalvar.Click += BtnSalvar_Click;
        btnCancelar = UITheme.CriarBotao("Cancelar", UITheme.TextMuted, true);
        btnCancelar.Width = 140;
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
                ? "Cliente sem pagamentos registrados."
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
