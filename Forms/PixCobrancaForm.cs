using System.Globalization;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Services;
using QRCoder;
using System.Text.Json.Nodes;

namespace ClientePagamentoSystem.Forms;

/// <summary>
/// Diálogo de cobrança Pix: mostra o QR Code com o valor da parcela e o
/// código "copia e cola". Chave/nome/cidade ficam salvos no appsettings.json.
/// </summary>
public class PixCobrancaForm : Form
{
    private readonly decimal _valor;
    private readonly string _descricao;
    private readonly string _txid;

    private TextBox txtChave = null!, txtNome = null!, txtCidade = null!, txtCodigo = null!;
    private PictureBox picQr = null!;

    public PixCobrancaForm(decimal valor, string descricao, string txid)
    {
        _valor = valor;
        _descricao = descricao;
        _txid = txid;
        Construir();
        CarregarConfig();
        GerarQr();
    }

    private void Construir()
    {
        Text = "💠 Cobrança Pix";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Card;
        ClientSize = new Size(460, 600);

        var lblTitulo = UITheme.CriarLabel("💠 Cobrança Pix", bold: true, cor: UITheme.Primary);
        lblTitulo.SetBounds(20, 14, 420, 24);

        var lblValor = UITheme.CriarLabel(
            $"{_descricao} — {_valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"))}",
            cor: UITheme.TextMuted);
        lblValor.SetBounds(20, 42, 420, 20);

        var lblChave = UITheme.CriarLabel("Chave Pix (CPF, e-mail, telefone ou aleatória):");
        lblChave.SetBounds(20, 70, 420, 20);
        txtChave = UITheme.CriarTextBox();
        txtChave.SetBounds(20, 92, 420, 28);
        txtChave.TextChanged += (_, _) => GerarQr();

        var lblNome = UITheme.CriarLabel("Nome do recebedor (como aparece no banco):");
        lblNome.SetBounds(20, 128, 420, 20);
        txtNome = UITheme.CriarTextBox();
        txtNome.SetBounds(20, 150, 250, 28);
        txtNome.TextChanged += (_, _) => GerarQr();

        var lblCidade = UITheme.CriarLabel("Cidade:");
        lblCidade.SetBounds(280, 128, 160, 20);
        txtCidade = UITheme.CriarTextBox();
        txtCidade.SetBounds(280, 150, 160, 28);
        txtCidade.TextChanged += (_, _) => GerarQr();

        picQr = new PictureBox
        {
            Bounds = new Rectangle(100, 190, 260, 260),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblCodigo = UITheme.CriarLabel("Código \"copia e cola\":");
        lblCodigo.SetBounds(20, 460, 420, 20);
        txtCodigo = new TextBox
        {
            Font = UITheme.FontPequena,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Bounds = new Rectangle(20, 482, 420, 50),
            BackColor = UITheme.ModoEscuro ? Color.FromArgb(15, 23, 42) : Color.White
        };

        var btnCopiar = UITheme.CriarBotao("📋 Copiar código", UITheme.Primary);
        btnCopiar.SetBounds(20, 540, 200, 36);
        btnCopiar.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                Clipboard.SetText(txtCodigo.Text);
                ErrorHelper.ShowSuccess("Código Pix copiado!", this);
            }
        };

        var btnSalvar = UITheme.CriarBotao("💾 Salvar dados", UITheme.Success);
        btnSalvar.SetBounds(228, 540, 130, 36);
        btnSalvar.Click += (_, _) => SalvarConfig();

        var btnFechar = UITheme.CriarBotao("Fechar", null, true);
        btnFechar.SetBounds(366, 540, 74, 36);
        btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblValor, lblChave, txtChave, lblNome, txtNome, lblCidade, txtCidade,
            picQr, lblCodigo, txtCodigo, btnCopiar, btnSalvar, btnFechar
        });

        AcceptButton = btnCopiar;
        CancelButton = btnFechar;
    }

    private string MontarPayload()
    {
        var chave = txtChave.Text.Trim();
        if (string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(txtNome.Text.Trim()))
            return "";
        return PixService.GerarPayload(chave, txtNome.Text, txtCidade.Text, _valor, _txid);
    }

    private void GerarQr()
    {
        var payload = MontarPayload();
        txtCodigo.Text = payload;
        if (string.IsNullOrEmpty(payload))
        {
            picQr.Image = null;
            return;
        }
        try
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var qr = new QRCode(data);
            picQr.Image = qr.GetGraphic(6, Color.Black, Color.White, true);
        }
        catch { picQr.Image = null; }
    }

    private void CarregarConfig()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(caminho)) return;
            var json = JsonNode.Parse(File.ReadAllText(caminho));
            txtChave.Text = json?["PixChave"]?.GetValue<string>() ?? "";
            txtNome.Text = json?["PixNome"]?.GetValue<string>() ?? "";
            txtCidade.Text = json?["PixCidade"]?.GetValue<string>() ?? "";
        }
        catch { /* ignora */ }
    }

    private void SalvarConfig()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var json = File.Exists(caminho)
                ? JsonNode.Parse(File.ReadAllText(caminho)) ?? new JsonObject()
                : new JsonObject();
            json["PixChave"] = txtChave.Text.Trim();
            json["PixNome"] = txtNome.Text.Trim();
            json["PixCidade"] = txtCidade.Text.Trim();
            File.WriteAllText(caminho,
                json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            ErrorHelper.ShowSuccess("Dados Pix salvos! Serão pré-preenchidos na próxima vez.", this);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível salvar os dados Pix.", ex, this);
        }
    }
}
