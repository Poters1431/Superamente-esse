using System.Drawing.Drawing2D;
using System.Text.Json.Nodes;

namespace ClientePagamentoSystem.Helpers;

/// <summary>Tema visual moderno: azul primário, fundo claro/escuro, cards e cantos arredondados.</summary>
public static class UITheme
{
    public static Color Primary { get; private set; } = Color.FromArgb(37, 99, 235);      // azul principal
    public static Color PrimaryDark { get; private set; } = Color.FromArgb(29, 78, 216);
    public static Color PrimaryLight { get; private set; } = Color.FromArgb(219, 234, 254);
    public static Color Background { get; private set; } = Color.FromArgb(243, 246, 251); // fundo claro
    public static Color Card { get; private set; } = Color.White;
    public static Color Sidebar { get; private set; } = Color.FromArgb(30, 58, 138);      // azul escuro
    public static Color SidebarHover { get; private set; } = Color.FromArgb(37, 99, 235);
    public static Color Text { get; private set; } = Color.FromArgb(31, 41, 55);
    public static Color TextMuted { get; private set; } = Color.FromArgb(107, 114, 128);
    public static Color Success { get; private set; } = Color.FromArgb(22, 163, 74);
    public static Color Warning { get; private set; } = Color.FromArgb(217, 119, 6);
    public static Color Danger { get; private set; } = Color.FromArgb(220, 38, 38);
    public static Color Border { get; private set; } = Color.FromArgb(229, 231, 235);
    public static Color GridAlt { get; private set; } = Color.FromArgb(248, 250, 252);    // linha alternada
    public static Color ChartTrack { get; private set; } = Color.FromArgb(235, 238, 244); // trilha do gráfico

    public static bool ModoEscuro { get; private set; }

    /// <summary>Aplica o tema claro ou escuro e (opcionalmente) persiste no appsettings.json.</summary>
    public static void AplicarTema(bool escuro, bool salvar = true)
    {
        ModoEscuro = escuro;
        if (escuro)
        {
            Primary = Color.FromArgb(59, 130, 246);
            PrimaryDark = Color.FromArgb(147, 197, 253);
            PrimaryLight = Color.FromArgb(30, 64, 175);
            Background = Color.FromArgb(15, 23, 42);   // slate-900
            Card = Color.FromArgb(30, 41, 59);         // slate-800
            Sidebar = Color.FromArgb(2, 6, 23);        // slate-950
            SidebarHover = Color.FromArgb(30, 64, 175);
            Text = Color.FromArgb(226, 232, 240);
            TextMuted = Color.FromArgb(148, 163, 184);
            Success = Color.FromArgb(34, 197, 94);
            Warning = Color.FromArgb(245, 158, 11);
            Danger = Color.FromArgb(248, 113, 113);
            Border = Color.FromArgb(51, 65, 85);
            GridAlt = Color.FromArgb(39, 51, 71);
            ChartTrack = Color.FromArgb(51, 65, 85);
        }
        else
        {
            Primary = Color.FromArgb(37, 99, 235);
            PrimaryDark = Color.FromArgb(29, 78, 216);
            PrimaryLight = Color.FromArgb(219, 234, 254);
            Background = Color.FromArgb(243, 246, 251);
            Card = Color.White;
            Sidebar = Color.FromArgb(30, 58, 138);
            SidebarHover = Color.FromArgb(37, 99, 235);
            Text = Color.FromArgb(31, 41, 55);
            TextMuted = Color.FromArgb(107, 114, 128);
            Success = Color.FromArgb(22, 163, 74);
            Warning = Color.FromArgb(217, 119, 6);
            Danger = Color.FromArgb(220, 38, 38);
            Border = Color.FromArgb(229, 231, 235);
            GridAlt = Color.FromArgb(248, 250, 252);
            ChartTrack = Color.FromArgb(235, 238, 244);
        }

        if (salvar) SalvarTemaNoConfig();
    }

    private static void SalvarTemaNoConfig()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(caminho)) return;
            var json = JsonNode.Parse(File.ReadAllText(caminho)) ?? new JsonObject();
            json["Tema"] = ModoEscuro ? "escuro" : "claro";
            File.WriteAllText(caminho, json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* não bloquear a UI se não conseguir salvar */ }
    }

    public static readonly Font FontTitulo = new("Segoe UI", 18f, FontStyle.Bold);
    public static readonly Font FontSubtitulo = new("Segoe UI", 12f, FontStyle.Bold);
    public static readonly Font FontNormal = new("Segoe UI", 10f, FontStyle.Regular);
    public static readonly Font FontPequena = new("Segoe UI", 9f, FontStyle.Regular);

    public static Button CriarBotao(string texto, Color? cor = null, bool outline = false)
    {
        var btn = new Button
        {
            Text = texto,
            Font = FontNormal,
            FlatStyle = FlatStyle.Flat,
            Height = 38,
            Cursor = Cursors.Hand,
            ForeColor = outline ? Primary : Color.White,
            BackColor = outline ? (ModoEscuro ? Card : Color.White) : (cor ?? Primary)
        };
        btn.FlatAppearance.BorderSize = outline ? 1 : 0;
        btn.FlatAppearance.BorderColor = Primary;
        btn.FlatAppearance.MouseOverBackColor = outline ? PrimaryLight :
            ControlPaint.Dark(btn.BackColor, 0.08f);
        return btn;
    }

    public static TextBox CriarTextBox(string? placeholder = null)
    {
        return new TextBox
        {
            Font = FontNormal,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ModoEscuro ? Color.FromArgb(15, 23, 42) : Color.White,
            ForeColor = Text,
            PlaceholderText = placeholder ?? string.Empty
        };
    }

    public static ComboBox CriarComboBox()
    {
        return new ComboBox
        {
            Font = FontNormal,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = ModoEscuro ? Color.FromArgb(15, 23, 42) : Color.White,
            ForeColor = Text
        };
    }

    public static void EstilizarGrid(DataGridView grid)
    {
        grid.BackgroundColor = Card;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        grid.ColumnHeadersHeight = 38;
        grid.DefaultCellStyle.Font = FontPequena;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlt;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.BackColor = Card;
        grid.RowHeadersDefaultCellStyle.BackColor = Card;
        grid.RowHeadersDefaultCellStyle.ForeColor = TextMuted;
        grid.RowHeadersDefaultCellStyle.SelectionBackColor = Card;
        grid.RowHeadersDefaultCellStyle.SelectionForeColor = TextMuted;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;
        grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
        grid.RowTemplate.Height = 36;
        grid.RowTemplate.DividerHeight = 1;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.GridColor = Border;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    public static Panel CriarCard() => new RoundedPanel
    {
        BackColor = Card,
        Padding = new Padding(16)
    };

    public static Label CriarLabel(string texto, bool bold = false, Color? cor = null)
    {
        return new Label
        {
            Text = texto,
            Font = bold ? FontSubtitulo : FontNormal,
            ForeColor = cor ?? Text,
            AutoSize = true
        };
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Panel com cantos arredondados e borda suave.</summary>
public class RoundedPanel : Panel
{
    public int Radius { get; set; } = 12;

    public RoundedPanel() => DoubleBuffered = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UITheme.RoundedRect(rect, Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        using var pen = new Pen(UITheme.Border, 1);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }
}
