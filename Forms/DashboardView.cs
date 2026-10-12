using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Repositories;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

public class DashboardView : UserControl
{
    public DashboardView()
    {
        BackColor = UITheme.Background;
        Load += (_, _) => Carregar();
    }

    private void Carregar()
    {
        Controls.Clear();
        try
        {
            var db = new DatabaseConnection(Program.ConnectionString);
            var clienteService = new ClienteService(new ClienteRepository(db));
            var pagService = new PagamentoService(
                new PagamentoRepository(db), new ClienteRepository(db), new FormaPagamentoRepository(db));

            var resumo = pagService.ObterResumo();
            int totalClientes = clienteService.ContarAtivos();
            var proximos = pagService.Listar()
                .Where(p => p.Status != "Pago")
                .OrderBy(p => p.DataVencimento)
                .Take(6)
                .ToList();

            decimal taxa = resumo.TotalPagamentos > 0
                ? 100m * resumo.Pagos / resumo.TotalPagamentos : 0m;
            var lblTitulo = new Label
            {
                Text = "📊 Dashboard",
                Font = UITheme.FontTitulo,
                ForeColor = UITheme.Text,
                Dock = DockStyle.Top,
                Height = 50
            };

            // ---------- Linha de cards ----------
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 260,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false,
                BackColor = UITheme.Background,
                Padding = new Padding(4)
            };

            flow.Controls.Add(CriarCard("👥", "Total de Clientes", totalClientes.ToString(), UITheme.Primary));
            flow.Controls.Add(CriarCard("💰", "Registros de Pagamento", resumo.TotalPagamentos.ToString(), UITheme.PrimaryDark));
            flow.Controls.Add(CriarCard("🧾", "Valor Total Cobrado", (resumo.ValorRecebido + resumo.ValorPendente).ToString("C2"), UITheme.PrimaryDark));
            flow.Controls.Add(CriarCard("✅", "Pagamentos Pagos", resumo.Pagos.ToString(), UITheme.Success));
            flow.Controls.Add(CriarCard("⏳", "Pendentes", resumo.Pendentes.ToString(), UITheme.Warning));
            flow.Controls.Add(CriarCard("⚠", "Atrasados", resumo.Atrasados.ToString(), UITheme.Danger));
            flow.Controls.Add(CriarCard("💵", "Valor Recebido", resumo.ValorRecebido.ToString("C2"), UITheme.Success));
            flow.Controls.Add(CriarCard("📄", "Valor Pendente", resumo.ValorPendente.ToString("C2"), UITheme.Warning));
            flow.Controls.Add(CriarCard("📈", "Taxa de Recebimento", taxa.ToString("F0") + "%", UITheme.Primary));

            // ---------- Corpo: gráfico + próximos vencimentos ----------
            var corpo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(4)
            };
            corpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            corpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

            corpo.Controls.Add(CriarGrafico(resumo));
            corpo.Controls.Add(CriarProximosVencimentos(proximos));

            Controls.Add(corpo);
            Controls.Add(flow);
            Controls.Add(lblTitulo);
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar o dashboard.", ex, this);
        }
    }

    private static Panel CriarCard(string icone, string titulo, string valor, Color cor)
    {
        var card = new RoundedPanel
        {
            Size = new Size(240, 118),
            Margin = new Padding(6),
            BackColor = UITheme.Card
        };

        var faixa = new Panel { BackColor = cor, Dock = DockStyle.Left, Width = 5 };
        card.Controls.Add(faixa);

        card.Controls.Add(new Label
        {
            Text = icone,
            Font = new Font("Segoe UI", 16f),
            ForeColor = cor,
            Location = new Point(20, 14),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = titulo,
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Location = new Point(64, 20),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = valor,
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            ForeColor = cor,
            Location = new Point(60, 52),
            AutoSize = true
        });
        return card;
    }

    private static Panel CriarGrafico(Repositories.DashboardResumo r)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = UITheme.Card,
            Padding = new Padding(20)
        };

        card.Controls.Add(new Label
        {
            Text = "Distribuição dos pagamentos",
            Font = UITheme.FontSubtitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 30
        });

        int total = r.Pagos + r.Pendentes + r.Atrasados;

        var area = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 0), BackColor = UITheme.Card };

        int y = 24;
        void Linha(string legenda, int qtd, Color cor)
        {
            double pct = total > 0 ? 100.0 * qtd / total : 0;
            var lbl = new Label
            {
                Text = $"{legenda}  —  {qtd} ({pct:F0}%)",
                Font = UITheme.FontNormal,
                ForeColor = UITheme.Text,
                Bounds = new Rectangle(4, y, 460, 22)
            };
            area.Controls.Add(lbl);
            y += 24;

            var barra = new Panel
            {
                BackColor = UITheme.ChartTrack,
                Bounds = new Rectangle(4, y, area.Width - 20, 24),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            var preench = new Panel
            {
                BackColor = cor,
                Dock = DockStyle.Left,
                Width = Math.Max(0, (int)((area.Width - 20) * (total > 0 ? qtd / (double)total : 0)))
            };
            barra.Controls.Add(preench);
            area.Controls.Add(barra);
            y += 38;
        }

        Linha("✅ Pagos", r.Pagos, UITheme.Success);
        Linha("⏳ Pendentes", r.Pendentes, UITheme.Warning);
        Linha("⚠ Atrasados", r.Atrasados, UITheme.Danger);

        card.Controls.Add(area);
        return card;
    }

    private static Panel CriarProximosVencimentos(List<Models.Pagamento> proximos)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            BackColor = UITheme.Card,
            Padding = new Padding(16)
        };

        card.Controls.Add(new Label
        {
            Text = "📅 Próximos vencimentos / em aberto",
            Font = UITheme.FontSubtitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 30
        });

        var grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "Cliente", FillWeight = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Valor", HeaderText = "Valor", FillWeight = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vencimento", HeaderText = "Vencimento", FillWeight = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 55 });
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Cells["Status"].Value is not string st) return;
            e.CellStyle.ForeColor = st == "Atrasado" ? UITheme.Danger : st == "Pago" ? UITheme.Success : UITheme.Warning;
            if (grid.Columns[e.ColumnIndex].Name == "Status")
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        };

        foreach (var p in proximos)
            grid.Rows.Add(p.ClienteNome, p.Valor.ToString("C2"),
                p.DataVencimento.ToString("dd/MM/yyyy"), p.Status);

        card.Controls.Add(grid);
        return card;
    }
}
