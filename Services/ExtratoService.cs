using System.Globalization;
using System.Text;
using ClientePagamentoSystem.Models;

namespace ClientePagamentoSystem.Services;

/// <summary>
/// Gera o extrato/recibo do cliente em HTML (abre no navegador, pronto para
/// imprimir ou salvar em PDF com Ctrl+P).
/// </summary>
public static class ExtratoService
{
    public static string GerarHtml(Cliente cliente, ResumoFinanceiroCliente resumo,
        List<Pagamento> pagamentos, string nomeUsuario)
    {
        var c = CultureInfo.GetCultureInfo("pt-BR");
        string Moeda(decimal v) => v.ToString("C2", c);
        string Enc(string s) => System.Net.WebUtility.HtmlEncode(s);

        var linhas = new StringBuilder();
        foreach (var p in pagamentos.OrderBy(p => p.DataVencimento))
        {
            var corStatus = p.Status switch
            {
                "Pago" => "#16a34a",
                "Atrasado" => "#dc2626",
                _ => "#d97706"
            };
            linhas.AppendLine($@"
                <tr>
                    <td>{p.DataVencimento:dd/MM/yyyy}</td>
                    <td class='dinheiro'>{Moeda(p.Valor)}</td>
                    <td>{(p.DataPagamento.HasValue ? p.DataPagamento.Value.ToString("dd/MM/yyyy") : "—")}</td>
                    <td>{Enc(p.FormaPagamentoNome)}</td>
                    <td><span class='badge' style='background:{corStatus}'>{p.Status}</span></td>
                    <td class='obs'>{Enc(p.Observacao ?? "")}</td>
                </tr>");
        }

        return $$"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
        <meta charset="utf-8">
        <title>Extrato — {{Enc(cliente.NomeCompleto)}}</title>
        <style>
            * { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; box-sizing: border-box; }
            body { background: #f3f6fb; padding: 32px; color: #1f2937; }
            .folha { max-width: 800px; margin: 0 auto; background: #fff; border-radius: 12px;
                     padding: 36px 40px; box-shadow: 0 2px 12px rgba(0,0,0,.08); }
            h1 { color: #2563eb; font-size: 22px; }
            .sub { color: #6b7280; font-size: 12px; margin-top: 4px; }
            .cliente { margin: 24px 0; padding: 16px; background: #f8fafc; border-radius: 8px;
                       border-left: 4px solid #2563eb; }
            .cliente b { font-size: 16px; }
            .cliente div { margin-top: 4px; font-size: 13px; color: #4b5563; }
            .cards { display: flex; gap: 12px; margin: 20px 0; }
            .card { flex: 1; border: 1px solid #e5e7eb; border-radius: 8px; padding: 12px 16px; }
            .card .titulo { font-size: 11px; color: #6b7280; text-transform: uppercase; }
            .card .valor { font-size: 18px; font-weight: bold; margin-top: 4px; }
            table { width: 100%; border-collapse: collapse; margin-top: 12px; font-size: 13px; }
            th { background: #2563eb; color: #fff; text-align: left; padding: 10px 8px; }
            td { padding: 9px 8px; border-bottom: 1px solid #e5e7eb; }
            tr:nth-child(even) td { background: #f8fafc; }
            .dinheiro { font-weight: 600; white-space: nowrap; }
            .badge { color: #fff; padding: 2px 10px; border-radius: 10px; font-size: 11px; font-weight: bold; }
            .obs { color: #6b7280; font-size: 12px; }
            .rodape { margin-top: 28px; text-align: center; color: #9ca3af; font-size: 11px; }
            @media print { body { background: #fff; padding: 0; } .folha { box-shadow: none; } }
        </style>
        </head>
        <body>
        <div class="folha">
            <h1>💳 ClientePagamento — Extrato do cliente</h1>
            <div class="sub">Emitido em {{DateTime.Now:dd/MM/yyyy HH:mm}} por {{Enc(nomeUsuario)}}</div>

            <div class="cliente">
                <b>{{Enc(cliente.NomeCompleto)}}</b>
                <div>CPF: {{Enc(cliente.CPF)}}
                    {{(string.IsNullOrWhiteSpace(cliente.Telefone) ? "" : " · Tel: " + Enc(cliente.Telefone))}}
                    {{(string.IsNullOrWhiteSpace(cliente.Email) ? "" : " · " + Enc(cliente.Email))}}
                </div>
            </div>

            <div class="cards">
                <div class="card"><div class="titulo">Total a pagar</div>
                    <div class="valor">{{Moeda(resumo.ValorTotal)}}</div></div>
                <div class="card"><div class="titulo">Total pago</div>
                    <div class="valor" style="color:#16a34a">{{Moeda(resumo.TotalPago)}}</div></div>
                <div class="card"><div class="titulo">Restante</div>
                    <div class="valor" style="color:#d97706">{{Moeda(resumo.ValorRestante)}}</div></div>
                <div class="card"><div class="titulo">Parcelas</div>
                    <div class="valor">{{resumo.ParcelasPagas}}/{{resumo.QuantidadeParcelas}} pagas</div></div>
            </div>

            <table>
                <tr><th>Vencimento</th><th>Valor</th><th>Pago em</th><th>Forma</th><th>Status</th><th>Observação</th></tr>
                {{linhas}}
            </table>

            <div class="rodape">Documento gerado pelo ClientePagamentoSystem</div>
        </div>
        </body>
        </html>
        """;
    }

    /// <summary>Salva o extrato em um arquivo temporário e abre no navegador padrão.</summary>
    public static void AbrirExtrato(Cliente cliente, ResumoFinanceiroCliente resumo,
        List<Pagamento> pagamentos, string nomeUsuario)
    {
        var html = GerarHtml(cliente, resumo, pagamentos, nomeUsuario);
        var caminho = Path.Combine(Path.GetTempPath(),
            $"extrato_{cliente.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        File.WriteAllText(caminho, html, Encoding.UTF8);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(caminho)
        {
            UseShellExecute = true
        });
    }
}
