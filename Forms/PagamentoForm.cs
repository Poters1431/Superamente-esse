using System.Globalization;
using ClientePagamentoSystem.Helpers;
using System.Text;
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

    // ---- Parcelamento no crédito ----
    private const int MaxParcelas = 20;
    private ComboBox cmbParcelas = null!;
    private NumericUpDown numSemJuros = null!;
    private TextBox txtJuros = null!;
    private Label lblSimulacao = null!;
    private Label lblRotuloCredito = null!;
    private Panel painelCredito = null!;

    /// <summary>Detecta forma de pagamento "crédito" (ignora acentos e maiúsculas).</summary>
    private static bool EhCredito(FormaPagamento? f)
    {
        if (f is null) return false;
        var n = f.Nome.Normalize(NormalizationForm.FormD);
        n = new string(n.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
        return n.Contains("credito", StringComparison.OrdinalIgnoreCase);
    }

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
        ClientSize = new Size(500, 810);
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
        txtValor.TextChanged += (_, _) => AtualizarSimulacao();
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
        cmbForma.SelectedIndexChanged += (_, _) => AtualizarPainelCredito();
        Linha(Rotulo("Forma de pagamento *"), cmbForma);

        // ---------- Parcelamento no crédito (visível só para "crédito") ----------
        painelCredito = new Panel { Dock = DockStyle.Fill, Visible = false };

        var lblCredroTitulo = UITheme.CriarLabel("💳 Parcelamento no crédito", bold: true);
        lblCredroTitulo.SetBounds(0, 2, 300, 20);

        var lblParcSel = UITheme.CriarLabel("Parcelas:");
        lblParcSel.SetBounds(0, 26, 62, 22);

        cmbParcelas = new ComboBox
        {
            Font = UITheme.FontNormal,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbParcelas.SetBounds(64, 22, 92, 28);
        for (int i = 1; i <= MaxParcelas; i++)
            cmbParcelas.Items.Add(i == 1 ? "1x à vista" : $"{i}x");
        cmbParcelas.SelectedIndex = 0;
        cmbParcelas.SelectedIndexChanged += (_, _) =>
        {
            int p = ParcelasSelecionadas();
            int sem = Math.Min((int)numSemJuros.Value, p);
            numSemJuros.Maximum = p;
            numSemJuros.Value = sem;
            AtualizarSimulacao();
        };

        var lblSemJuros = UITheme.CriarLabel("Sem juros até:");
        lblSemJuros.SetBounds(164, 26, 94, 22);

        numSemJuros = new NumericUpDown
        {
            Font = UITheme.FontNormal,
            Minimum = 0,
            Maximum = MaxParcelas,
            Value = 0,
            TextAlign = HorizontalAlignment.Center
        };
        numSemJuros.SetBounds(260, 22, 52, 28);
        numSemJuros.ValueChanged += (_, _) => AtualizarSimulacao();

        var lblJuros = UITheme.CriarLabel("Juros (% a.m.):");
        lblJuros.SetBounds(320, 26, 94, 22);

        txtJuros = UITheme.CriarTextBox("0");
        txtJuros.TextAlign = HorizontalAlignment.Right;
        txtJuros.SetBounds(416, 22, 52, 28);
        txtJuros.TextChanged += (_, _) => AtualizarSimulacao();

        lblSimulacao = new Label
        {
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = UITheme.Primary,
            Bounds = new Rectangle(0, 58, 468, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };

        painelCredito.Controls.AddRange(new Control[]
        {
            lblCredroTitulo, lblParcSel, cmbParcelas, lblSemJuros, numSemJuros,
            lblJuros, txtJuros, lblSimulacao
        });

        lblRotuloCredito = Rotulo("Parcelas / juros (crédito)");
        lblRotuloCredito.Visible = false;
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
        tabela.Controls.Add(lblRotuloCredito);
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 104f));
        tabela.Controls.Add(painelCredito);

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
        AtualizarPainelCredito();
    }

    private int ParcelasSelecionadas()
    {
        if (cmbParcelas.SelectedIndex >= 0) return cmbParcelas.SelectedIndex + 1;
        return 1;
    }

    private void AtualizarPainelCredito()
    {
        bool credito = _pagamento is null && EhCredito(cmbForma.SelectedItem as FormaPagamento);
        painelCredito.Visible = credito;
        lblRotuloCredito.Visible = credito;
        if (credito) AtualizarSimulacao();
    }

    /// <summary>
    /// Calcula e exibe automaticamente o total com juros e o valor de cada
    /// parcela conforme o valor, número de parcelas, carência sem juros e taxa.
    /// Juros compostos aplicados apenas quando parcelas > "sem juros até".
    /// </summary>
    private void AtualizarSimulacao()
    {
        if (painelCredito is null || !painelCredito.Visible) return;
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        if (!decimal.TryParse(txtValor.Text.Replace("R$", "").Trim(),
                NumberStyles.Currency, cultura, out var valor) || valor <= 0)
        {
            lblSimulacao.Text = "Informe o valor para simular o parcelamento.";
            lblSimulacao.ForeColor = UITheme.TextMuted;
            return;
        }

        int parcelas = ParcelasSelecionadas();
        int semJuros = Math.Min((int)numSemJuros.Value, parcelas);
        decimal taxa = 0m;
        decimal.TryParse(txtJuros.Text.Replace("%", "").Trim(),
            NumberStyles.Number, cultura, out taxa);

        decimal total = valor;
        bool comJuros = parcelas > semJuros && taxa > 0;
        if (comJuros)
        {
            int periodos = parcelas - semJuros;
            total = valor * (decimal)Math.Pow(1 + (double)taxa / 100.0, periodos);
            total = Math.Round(total, 2);
        }

        decimal valorParcela = Math.Round(total / parcelas, 2);
        lblSimulacao.ForeColor = comJuros ? UITheme.Danger : UITheme.Primary;
        lblSimulacao.Text = parcelas <= 1
            ? $"💳 Pagamento à vista: {valor.ToString("C2", cultura)}"
            : comJuros
                ? $"💳 Total: {total.ToString("C2", cultura)}, em {parcelas}x de {valorParcela.ToString("C2", cultura)} " +
                  $"(juros de {taxa.ToString("N2", cultura)}% a.m. após {semJuros}x)"
                : $"💳 Total: {total.ToString("C2", cultura)}, em {parcelas}x de {valorParcela.ToString("C2", cultura)} sem juros";
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

    /// <summary>
    /// Gera N pagamentos mensais (parcelas) para compra no crédito,
    /// aplicando juros compostos quando passa da carência "sem juros até".
    /// A última parcela ajusta os centavos para o total fechar exato.
    /// </summary>
    private void RegistrarParcelamento(Cliente cliente, FormaPagamento forma, decimal valor, int parcelas)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        int semJuros = Math.Min((int)numSemJuros.Value, parcelas);
        decimal.TryParse(txtJuros.Text.Replace("%", "").Trim(),
            NumberStyles.Number, cultura, out decimal taxa);

        decimal total = valor;
        if (parcelas > semJuros && taxa > 0)
            total = Math.Round(valor * (decimal)Math.Pow(1 + (double)taxa / 100.0, parcelas - semJuros), 2);

        decimal baseParcela = Math.Round(total / parcelas, 2);
        string status = cmbStatus.SelectedItem?.ToString() ?? "Pendente";
        DateTime? dataPagamento = chkPago.Checked && dtpPagamento.Enabled
            ? dtpPagamento.Value
            : status == "Pago" ? DateTime.Now : null;
        string baseObs = string.IsNullOrWhiteSpace(txtObservacao.Text)
            ? "" : txtObservacao.Text.Trim() + " ";

        decimal acumulado = 0m;
        for (int i = 1; i <= parcelas; i++)
        {
            decimal v = i == parcelas ? total - acumulado : baseParcela;
            acumulado += v;
            _service.Registrar(new Pagamento
            {
                ClienteId = cliente.Id,
                Valor = v,
                DataVencimento = dtpVencimento.Value.Date.AddMonths(i - 1),
                Status = status,
                FormaPagamentoId = forma.Id,
                Observacao = $"{baseObs}(Parcela {i}/{parcelas})".Trim(),
                UsuarioId = _usuario.Id,
                DataPagamento = dataPagamento
            });
        }

        ErrorHelper.ShowSuccess(
            $"Parcelamento registrado: {parcelas}x de {(Math.Round(total / parcelas, 2)).ToString("C2", cultura)} " +
            $"— total {total.ToString("C2", cultura)}.", this);
        DialogResult = DialogResult.OK;
        Close();
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

            // Parcelamento no crédito: gera N pagamentos mensais de uma vez
            int parcelas = painelCredito.Visible ? ParcelasSelecionadas() : 1;
            if (_pagamento is null && parcelas > 1)
            {
                RegistrarParcelamento(cliente, forma, valor, parcelas);
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
        catch (ArgumentNullException ex)
        {
            ErrorHelper.ShowWarning(ex.Message, this);
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
