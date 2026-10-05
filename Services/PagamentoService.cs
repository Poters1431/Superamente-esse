using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;

namespace ClientePagamentoSystem.Services;

public class PagamentoService
{
    private readonly PagamentoRepository _repo;
    private readonly ClienteRepository _clientes;
    private readonly FormaPagamentoRepository _formas;

    public PagamentoService(PagamentoRepository repo, ClienteRepository clientes,
        FormaPagamentoRepository formas)
    {
        _repo = repo;
        _clientes = clientes;
        _formas = formas;
    }

    public static readonly string[] StatusValidos = { "Pendente", "Pago", "Atrasado" };

    public List<Pagamento> Listar(string? filtro = null, int? clienteId = null,
        string? status = null, int? formaPagamentoId = null)
    {
        var lista = _repo.Listar(filtro, clienteId, status, formaPagamentoId);
        // Atualiza automaticamente para 'Atrasado' o que está pendente e vencido
        foreach (var p in lista)
        {
            if (p.Status == "Pendente" && p.DataVencimento.Date < DateTime.Today)
            {
                p.Status = "Atrasado";
                _repo.Atualizar(p);
            }
        }
        return lista;
    }

    public Pagamento? ObterPorId(int id) => _repo.ObterPorId(id);

    public int Registrar(Pagamento p)
    {
        Validar(p);
        return _repo.Inserir(p);
    }

    public void Atualizar(Pagamento p)
    {
        if (p.Id <= 0) throw new ArgumentException("Pagamento inválido para edição.");
        Validar(p);
        _repo.Atualizar(p);
    }

    public void Excluir(int id) => _repo.Desativar(id); // exclusão lógica, preserva histórico

    public DashboardResumo ObterResumo() => _repo.ObterResumo();

    /// <summary>
    /// Identifica baixas rápidas (Descontar): esses valores são pagamentos
    /// avulsos e NÃO entram no "Total a pagar" do cliente; eles reduzem o restante.
    /// </summary>
    private static bool EhBaixaRapida(Pagamento p)
    {
        var obs = p.Observacao ?? "";
        return obs.StartsWith(Pagamento.BaixaRapidaPrefixo, StringComparison.OrdinalIgnoreCase)
            || obs.StartsWith("Baixa rápida", StringComparison.OrdinalIgnoreCase); // registros antigos
    }

    public ResumoFinanceiroCliente ObterResumoCliente(int clienteId)
    {
        var pagamentos = _repo.Listar(clienteId: clienteId);
        var baixas = pagamentos.Where(EhBaixaRapida).ToList();
        var parcelas = pagamentos.Where(p => !EhBaixaRapida(p)).ToList();
        decimal totalDescontado = baixas.Sum(p => p.Valor);

        return new ResumoFinanceiroCliente
        {
            ValorTotal = parcelas.Sum(p => p.Valor),
            QuantidadeParcelas = parcelas.Count,
            ParcelasPagas = parcelas.Count(p => p.Status == "Pago"),
            TotalPago = pagamentos.Where(p => p.Status == "Pago").Sum(p => p.Valor),
            ValorRestante = Math.Max(0m, parcelas.Where(p => p.Status != "Pago").Sum(p => p.Valor) - totalDescontado)
        };
    }

    private void Validar(Pagamento p)
    {
        if (p.ClienteId <= 0 || _clientes.ObterPorId(p.ClienteId) is null)
            throw new ArgumentException("Selecione um cliente válido.");

        if (p.Valor <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.");

        if (p.FormaPagamentoId <= 0 || _formas.ObterPorId(p.FormaPagamentoId) is null)
            throw new ArgumentException("Selecione uma forma de pagamento válida.");

        if (!StatusValidos.Contains(p.Status))
            throw new ArgumentException("Status inválido. Use: Pendente, Pago ou Atrasado.");

        // Regra: status 'Pago' exige data de pagamento; se pago sem data, assume agora.
        if (p.Status == "Pago" && p.DataPagamento is null)
            p.DataPagamento = DateTime.Now;
        if (p.Status != "Pago")
            p.DataPagamento = null;
    }
}

public class ResumoFinanceiroCliente
{
    public decimal ValorTotal { get; set; }
    public int QuantidadeParcelas { get; set; }
    public int ParcelasPagas { get; set; }
    public decimal TotalPago { get; set; }
    public decimal ValorRestante { get; set; }
    public string Status =>
        QuantidadeParcelas == 0 ? "Sem pagamentos" :
        ValorRestante <= 0 ? "Quitado" :
        TotalPago > 0 ? "Parcialmente pago" : "Em aberto";
}
