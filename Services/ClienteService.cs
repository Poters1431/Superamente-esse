using ClientePagamentoSystem.Helpers;
using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;

namespace ClientePagamentoSystem.Services;

public class ClienteService
{
    private readonly ClienteRepository _repo;

    public ClienteService(ClienteRepository repo) => _repo = repo;

    public List<Cliente> Listar(string? filtro = null, bool somenteAtivos = false)
        => _repo.Listar(filtro, somenteAtivos);

    public List<Cliente> ListarAtivos() => _repo.Listar(null, true);

    public Cliente? ObterPorId(int id) => _repo.ObterPorId(id);

    public int Cadastrar(Cliente c)
    {
        Validar(c, 0);
        return _repo.Inserir(c);
    }

    public void Atualizar(Cliente c)
    {
        if (c.Id <= 0) throw new ArgumentException("Cliente inválido para edição.");
        Validar(c, c.Id);
        _repo.Atualizar(c);
    }

    public void Desativar(int id) => _repo.Desativar(id);

    public void Reativar(int id) => _repo.Reativar(id);

    public int ContarAtivos() => _repo.Contar(true);

    private void Validar(Cliente c, int ignorarId)
    {
        if (string.IsNullOrWhiteSpace(c.NomeCompleto))
            throw new ArgumentException("O nome do cliente é obrigatório.");

        c.NomeCompleto = c.NomeCompleto.Trim();
        c.CPF = ValidationHelper.SomenteNumeros(c.CPF);

        if (!ValidationHelper.CpfValido(c.CPF))
            throw new ArgumentException("CPF inválido. Verifique os números digitados.");

        if (_repo.ExisteCpf(c.CPF, ignorarId))
            throw new ArgumentException("Já existe um cliente cadastrado com este CPF.");

        if (!string.IsNullOrWhiteSpace(c.Email) && !ValidationHelper.EmailValido(c.Email))
            throw new ArgumentException("E-mail inválido. Verifique o endereço digitado.");
    }
}
