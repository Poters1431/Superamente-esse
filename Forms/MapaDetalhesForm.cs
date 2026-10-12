using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;

namespace ClientePagamentoSystem.Forms;

/// <summary>Lista os pagamentos de um cliente em um mês específico do mapa mensal.</summary>
public class MapaDetalhesForm : Form
{
    public MapaDetalhesForm(string nomeCliente, string mes, int ano, List<Pagamento> pagamentos)
    {
        Text = $"Pagamentos — {nomeCliente} ({mes}/{ano})";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.Background;
        ClientSize = new Size(700, 420);

        var lblTitulo = UITheme.CriarLabel($"💳 {nomeCliente} — {mes}/{ano}", bold: true,
            cor: UITheme.Primary);
        lblTitulo.SetBounds(16, 12, 660, 24);

        var grid = new DataGridView { Dock = DockStyle.Fill };
        UITheme.EstilizarGrid(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Vencimento", FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Valor", FillWeight = 65 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Pago em", FillWeight = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Forma", FillWeight = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", FillWeight = 55 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Observação", FillWeight = 160 });

        foreach (var p in pagamentos.OrderBy(p => p.DataVencimento))
            grid.Rows.Add(p.DataVencimento.ToString("dd/MM/yyyy"), p.Valor.ToString("C2"),
                p.DataPagamento?.ToString("dd/MM/yyyy") ?? "—",
                p.FormaPagamentoNome, p.Status, p.Observacao ?? "");

        if (pagamentos.Count == 0)
            grid.Rows.Add("—", "—", "—", "—", "Nenhum registro neste mês", "");

        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Rows[e.RowIndex].Cells[4].Value is not string st) return;
            e.CellStyle!.ForeColor = st switch
            {
                "Pago" => UITheme.Success,
                "Atrasado" => UITheme.Danger,
                "Pendente" => UITheme.Warning,
                _ => e.CellStyle.ForeColor
            };
        };

        var card = UITheme.CriarCard();
        card.SetBounds(16, 44, 668, 320);
        card.Controls.Add(grid);

        var btnFechar = UITheme.CriarBotao("Fechar", UITheme.TextMuted, true);
        btnFechar.SetBounds(560, 372, 124, 36);
        btnFechar.Click += (_, _) => Close();

        Controls.AddRange(new Control[] { lblTitulo, card, btnFechar });
        CancelButton = btnFechar;
    }
}
