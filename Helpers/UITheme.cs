using System.Drawing.Drawing2D;

namespace ClientePagamentoSystem.Helpers;

/// <summary>Tema visual moderno: azul primário, fundo claro, cards e cantos arredondados.</summary>
public static class UITheme
{
    public static readonly Color Primary = Color.FromArgb(37, 99, 235);      // azul principal
    public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);
    public static readonly Color PrimaryLight = Color.FromArgb(219, 234, 254);
    public static readonly Color Background = Color.FromArgb(243, 246, 251); // fundo claro
    public static readonly Color Card = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(30, 58, 138);      // azul escuro
    public static readonly Color SidebarHover = Color.FromArgb(37, 99, 235);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color Warning = Color.FromArgb(217, 119, 6);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Border = Color.FromArgb(229, 231, 235);

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
            BackColor = outline ? Color.White : (cor ?? Primary)
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
            BackColor = Color.White,
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
            BackColor = Color.White,
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
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
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
