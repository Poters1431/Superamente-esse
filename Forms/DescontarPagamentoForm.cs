using System.Globalization;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>
/// Diálogo rápido de baixa: informa o valor recebido, escolhe a forma de
/// pagamento e confirma. O valor é descontado imediatamente do restante do
/// cliente, sem precisar abrir o formulário completo de pagamentos.
/// </summary>
public class DescontarPagamentoForm : Form
{
    private readonly PagamentoService _pagService;
    private readonly Cliente _cliente;
    private readonly Usuario _usuario;
    private readonly decimal _valorRestante;
    private readonly List<FormaPagamento> _formas;

    private TextBox txtValor = null!;
    private ComboBox cmbForma = null!;
    private TextBox txtObservacao = null!;

    public DescontarPagamentoForm(PagamentoService pagService, Cliente cliente,
        Usuario usuario, decimal valorRestante, List<FormaPagamento> formas)
    {
        _pagService = pagService;
        _cliente = cliente;
        _usuario = usuario;
        _valorRestante = valorRestante;
        _formas = formas;
        Construir();
    }

    private void Construir()
    {
        Text = "Descontar pagamento";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Card;
        ClientSize = new Size(400, 340);

        var lblTitulo = UITheme.CriarLabel("💸 Descontar do cliente", bold: true);
        lblTitulo.SetBounds(20, 16, 360, 24);

        var lblCliente = UITheme.CriarLabel(_cliente.NomeCompleto, cor: UITheme.Primary);
        lblCliente.SetBounds(20, 46, 360, 22);

        var lblRestante = UITheme.CriarLabel($"Restante: {_valorRestante.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"))}",
            cor: _valorRestante > 0 ? UITheme.Warning : UITheme.Success);
        lblRestante.SetBounds(20, 70, 360, 22);

        var lblValor = UITheme.CriarLabel("Valor a descontar (R$):");
        lblValor.SetBounds(20, 104, 360, 20);

        txtValor = UITheme.CriarTextBox();
        txtValor.SetBounds(20, 126, 360, 28);
        txtValor.Text = _valorRestante > 0 ? _valorRestante.ToString("N2") : "0,00";
        txtValor.SelectAll();

        var lblForma = UITheme.CriarLabel("Forma de pagamento:");
        lblForma.SetBounds(20, 164, 360, 20);

        cmbForma = UITheme.CriarComboBox();
        cmbForma.SetBounds(20, 186, 360, 28);
        foreach (var f in _formas) cmbForma.Items.Add(f);
        if (cmbForma.Items.Count > 0) cmbForma.SelectedIndex = 0;

        var lblObs = UITheme.CriarLabel("Observação (opcional):");
        lblObs.SetBounds(20, 224, 360, 20);

        txtObservacao = UITheme.CriarTextBox();
        txtObservacao.SetBounds(20, 246, 360, 28);

        var btnConfirmar = UITheme.CriarBotao("✔ Confirmar baixa", UITheme.Success);
        btnConfirmar.SetBounds(20, 288, 220, 38);
        btnConfirmar.Click += BtnConfirmar_Click;

        var btnCancelar = UITheme.CriarBotao("Cancelar", null, true);
        btnCancelar.SetBounds(248, 288, 132, 38);
        btnCancelar.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblCliente, lblRestante, lblValor, txtValor,
            lblForma, cmbForma, lblObs, txtObservacao, btnConfirmar, btnCancelar
        });

        AcceptButton = btnConfirmar;
        CancelButton = btnCancelar;
    }

    private void BtnConfirmar_Click(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(txtValor.Text, NumberStyles.Number,
                CultureInfo.GetCultureInfo("pt-BR"), out var valor) || valor <= 0)
        {
            ErrorHelper.ShowWarning("Informe um valor válido maior que zero.", this);
            txtValor.Focus();
            return;
        }

        if (cmbForma.SelectedItem is not FormaPagamento forma)
        {
            ErrorHelper.ShowWarning("Selecione uma forma de pagamento.", this);
            return;
        }

        try
        {
            _pagService.Registrar(new Pagamento
            {
                ClienteId = _cliente.Id,
                Valor = valor,
                DataVencimento = DateTime.Today,
                Status = "Pago",
                FormaPagamentoId = forma.Id,
                Observacao = string.IsNullOrWhiteSpace(txtObservacao.Text)
                    ? $"{Pagamento.BaixaRapidaPrefixo} Baixa rápida — {_cliente.NomeCompleto}"
                    : $"{Pagamento.BaixaRapidaPrefixo} {txtObservacao.Text.Trim()}",
                UsuarioId = _usuario.Id
            });

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível descontar o valor.", ex, this);
        }
    }
}
