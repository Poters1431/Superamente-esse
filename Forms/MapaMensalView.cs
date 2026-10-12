using System.Globalization;
using ClientePagamentoSystem.Data;
using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;
using ClientePagamentoSystem.Services;

namespace ClientePagamentoSystem.Forms;

/// <summary>
/// Mapa mensal de pagamentos (estilo caderno de controle): linhas = clientes,
/// colunas = meses do ano, célula = valores pagos no mês.
/// Verde = pago · Vermelho = a cobrar · vazio = nada previsto.
/// </summary>
public class MapaMensalView : UserControl
{
    private static readonly string[] Meses =
        { "JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ" };

    private DataGridView grid = null!;
    private Label lblAno = null!, lblLegenda = null!;
    private int _ano = DateTime.Today.Year;

    private ClienteService _clienteService = null!;
    private PagamentoService _pagService = null!;

    public MapaMensalView()
    {
        ConstruirInterface();
        Load += (_, _) => Carregar();
    }

    private void ConstruirInterface()
    {
        BackColor = UITheme.Background;

        var lblTitulo = new Label
        {
            Text = "📅 Mapa Mensal de Pagamentos",
            Font = UITheme.FontTitulo,
            ForeColor = UITheme.Text,
            Dock = DockStyle.Top,
            Height = 50
        };

        // ---------- Barra de ano + legenda ----------
        var cardTopo = UITheme.CriarCard();
        cardTopo.Dock = DockStyle.Top;
        cardTopo.Height = 68;
        cardTopo.Padding = new Padding(12, 8, 12, 8);

        var btnAnterior = UITheme.CriarBotao("◀", null, true);
        btnAnterior.SetBounds(12, 14, 50, 36);
        btnAnterior.Click += (_, _) => { _ano--; Carregar(); };

        lblAno = new Label
        {
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = UITheme.Primary,
            TextAlign = ContentAlignment.MiddleCenter,
            Bounds = new Rectangle(70, 14, 110, 36)
        };

        var btnProximo = UITheme.CriarBotao("▶", null, true);
        btnProximo.SetBounds(188, 14, 50, 36);
        btnProximo.Click += (_, _) => { if (_ano < DateTime.Today.Year + 1) _ano++; Carregar(); };

        lblLegenda = new Label
        {
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Bounds = new Rectangle(258, 14, 780, 36),
            Text = "🟩 recebido   🟥 a cobrar   🟧 recebido parcial   ·   pagamento novo já aparece aqui (duplo clique = detalhes)"
        };

        cardTopo.Controls.AddRange(new Control[] { btnAnterior, lblAno, btnProximo, lblLegenda });

        // ---------- Grade clientes × meses (estilo caderno) ----------
        grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.RowHeadersVisible = true;                 // numera as linhas como no caderno
        grid.RowHeadersWidth = 42;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cliente", HeaderText = "CLIENTE", FillWeight = 150 });
        foreach (var m in Meses)
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = m, HeaderText = m, FillWeight = 58,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Total", HeaderText = "TOTAL", FillWeight = 72,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
        });
        grid.Columns["Cliente"]!.Frozen = true;        // coluna dos nomes fixa ao rolar
        grid.CellFormatting += Grid_CellFormatting;
        grid.CellDoubleClick += Grid_CellDoubleClick;
        grid.RowsAdded += (_, e) =>
        {
            for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
                grid.Rows[i].HeaderCell.Value = (i + 1).ToString();
        };

        var cardGrid = UITheme.CriarCard();
        cardGrid.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(cardTopo);
        Controls.Add(lblTitulo);
    }

    private void Carregar()
    {
        try
        {
            var db = new DatabaseConnection(Program.ConnectionString);
            _clienteService = new ClienteService(new ClienteRepository(db));
            _pagService = new PagamentoService(
                new PagamentoRepository(db), new ClienteRepository(db), new FormaPagamentoRepository(db));

            lblAno.Text = _ano.ToString();
            var mapa = _pagService.ObterMapaMensal(_ano);
            var clientes = _clienteService.ListarAtivos().OrderBy(c => c.NomeCompleto).ToList();

            grid.Rows.Clear();
            var totalMes = new decimal[12];
            decimal totalGeral = 0m;

            foreach (var c in clientes)
            {
                int row = grid.Rows.Add();
                grid.Rows[row].Tag = c;
                grid.Rows[row].Cells[0].Value = c.NomeCompleto;
                decimal totalCliente = 0m;

                for (int mes = 1; mes <= 12; mes++)
                {
                    mapa.TryGetValue((c.Id, mes), out var v);
                    var cell = grid.Rows[row].Cells[mes];
                    cell.Tag = (v.pago, v.devido);
                    if (v.pago > 0 && v.devido > 0)
                        cell.Value = $"{v.pago.ToString("N0")} +{v.devido.ToString("N0")}";
                    else if (v.pago > 0)
                        cell.Value = v.pago.ToString("N0");
                    else if (v.devido > 0)
                        cell.Value = v.devido.ToString("N0");
                    totalCliente += v.pago;
                    totalMes[mes - 1] += v.pago;
                }

                grid.Rows[row].Cells[13].Value = totalCliente > 0 ? totalCliente.ToString("N0") : "";
                totalGeral += totalCliente;
            }

            // Linha de totais do mês
            int rowTotal = grid.Rows.Add();
            grid.Rows[rowTotal].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.Rows[rowTotal].DefaultCellStyle.BackColor = UITheme.PrimaryLight;
            grid.Rows[rowTotal].Cells[0].Value = "TOTAL DO MÊS";
            for (int mes = 1; mes <= 12; mes++)
                grid.Rows[rowTotal].Cells[mes].Value = totalMes[mes - 1] > 0 ? totalMes[mes - 1].ToString("N0") : "";
            grid.Rows[rowTotal].Cells[13].Value = totalGeral > 0 ? totalGeral.ToString("N0") : "";
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar o mapa mensal.", ex, this);
        }
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 1 || e.ColumnIndex > 12) return;
        if (grid.Rows[e.RowIndex].Tag is not Cliente) return; // linha de total
        if (grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is not (decimal pago, decimal devido)) return;

        bool escuro = UITheme.ModoEscuro;
        if (pago > 0 && devido > 0)
        {
            // Recebeu, mas ainda tem valor em aberto no mês
            e.CellStyle!.BackColor = escuro ? Color.FromArgb(120, 53, 15) : Color.FromArgb(254, 243, 199);
            e.CellStyle.ForeColor = escuro ? Color.FromArgb(252, 211, 77) : UITheme.Warning;
            e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }
        else if (pago > 0)
        {
            e.CellStyle!.BackColor = escuro ? Color.FromArgb(20, 83, 45) : Color.FromArgb(220, 252, 231);
            e.CellStyle.ForeColor = escuro ? Color.FromArgb(134, 239, 172) : UITheme.Success;
            e.CellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }
        else if (devido > 0)
        {
            e.CellStyle!.BackColor = escuro ? Color.FromArgb(127, 29, 29) : Color.FromArgb(254, 226, 226);
            e.CellStyle.ForeColor = escuro ? Color.FromArgb(252, 165, 165) : UITheme.Danger;
        }

        grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
            pago > 0 || devido > 0
                ? $"{Meses[e.ColumnIndex - 1]}/{_ano}: recebido R$ {pago:N2} · a cobrar R$ {devido:N2}\n(duplo clique para ver os pagamentos)"
                : "";
    }

    /// <summary>Duplo clique na célula → detalhes dos pagamentos daquele mês.</summary>
    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 1 || e.ColumnIndex > 12) return;
        if (grid.Rows[e.RowIndex].Tag is not Cliente cliente) return;
        int mes = e.ColumnIndex;

        var pagamentos = _pagService.Listar(clienteId: cliente.Id, somenteClientesAtivos: false)
            .Where(p =>
                (p.Status == "Pago" && p.DataPagamento.HasValue &&
                 p.DataPagamento.Value.Year == _ano && p.DataPagamento.Value.Month == mes) ||
                (p.Status != "Pago" && p.DataVencimento.Year == _ano && p.DataVencimento.Month == mes))
            .ToList();

        using var form = new MapaDetalhesForm(cliente.NomeCompleto, Meses[mes - 1], _ano, pagamentos);
        form.ShowDialog(this);
    }
}
