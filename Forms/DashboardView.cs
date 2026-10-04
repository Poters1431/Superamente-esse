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

            var lblTitulo = new Label
            {
                Text = "📊 Dashboard",
                Font = UITheme.FontTitulo,
                ForeColor = UITheme.Text,
                Dock = DockStyle.Top,
                Height = 50
            };
            Controls.Add(lblTitulo);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true
            };

            flow.Controls.Add(CriarCard("👥 Total de Clientes", totalClientes.ToString(), UITheme.Primary));
            flow.Controls.Add(CriarCard("💰 Total de Pagamentos", resumo.TotalPagamentos.ToString(), UITheme.PrimaryDark));
            flow.Controls.Add(CriarCard("✅ Pagamentos Pagos", resumo.Pagos.ToString(), UITheme.Success));
            flow.Controls.Add(CriarCard("⏳ Pagamentos Pendentes", resumo.Pendentes.ToString(), UITheme.Warning));
            flow.Controls.Add(CriarCard("⚠ Pagamentos Atrasados", resumo.Atrasados.ToString(), UITheme.Danger));
            flow.Controls.Add(CriarCard("💵 Valor Recebido", resumo.ValorRecebido.ToString("C2"), UITheme.Success));
            flow.Controls.Add(CriarCard("📄 Valor Pendente", resumo.ValorPendente.ToString("C2"), UITheme.Warning));

            // Gráfico simples: barra de proporção pago/pendente/atrasado
            var grafico = CriarGrafico(resumo);
            flow.Controls.Add(grafico);

            Controls.Add(flow);
            lblTitulo.BringToFront();
        }
        catch (Exception ex)
        {
            ErrorHelper.ShowError("Não foi possível carregar o dashboard.", ex, this);
        }
    }

    private static Panel CriarCard(string titulo, string valor, Color cor)
    {
        var card = new RoundedPanel
        {
            Size = new Size(250, 110),
            Margin = new Padding(10),
            BackColor = UITheme.Card
        };
        card.Controls.Add(new Label
        {
            Text = titulo,
            Font = UITheme.FontPequena,
            ForeColor = UITheme.TextMuted,
            Location = new Point(16, 16),
            AutoSize = true
        });
        card.Controls.Add(new Label
        {
            Text = valor,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = cor,
            Location = new Point(16, 44),
            AutoSize = true
        });
        return card;
    }

    private static Panel CriarGrafico(Repositories.DashboardResumo r)
    {
        var card = new RoundedPanel
        {
            Size = new Size(780, 130),
            Margin = new Padding(10),
            BackColor = UITheme.Card
        };
        card.Controls.Add(UITheme.CriarLabel("Distribuição dos pagamentos", true));
        card.Controls[0].Location = new Point(16, 12);

        int total = Math.Max(1, r.Pagos + r.Pendentes + r.Atrasados);
        int larguraTotal = 740;
        int y = 60, altura = 34, x = 16;

        void Barra(int qtd, Color cor, string legenda)
        {
            if (qtd <= 0) return;
            int larg = Math.Max(4, (int)(larguraTotal * (qtd / (double)total)));
            var barra = new Panel { BackColor = cor, Bounds = new Rectangle(x, y, larg, altura) };
            card.Controls.Add(barra);
            card.Controls.Add(new Label
            {
                Text = $"{legenda}: {qtd}",
                Font = UITheme.FontPequena,
                ForeColor = Color.White,
                BackColor = cor,
                Location = new Point(x + 8, y + 8),
                AutoSize = true
            });
            x += larg;
        }

        Barra(r.Pagos, UITheme.Success, "Pagos");
        Barra(r.Pendentes, UITheme.Warning, "Pendentes");
        Barra(r.Atrasados, UITheme.Danger, "Atrasados");
        return card;
    }
}
