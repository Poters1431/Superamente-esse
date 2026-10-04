using ClientePagamentoSystem.Models;
using ClientePagamentoSystem.Repositories;

namespace ClientePagamentoSystem.Services;

public class AuthService
{
    private readonly UsuarioRepository _usuarios;

    public AuthService(UsuarioRepository usuarios) => _usuarios = usuarios;

    /// <summary>
    /// Autentica o usuário consultando o banco e validando a senha
    /// com BCrypt.Net.BCrypt.Verify(). Nunca compara senha em texto.
    /// </summary>
    public Usuario Autenticar(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Informe o usuário.");
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Informe a senha.");

        var usuario = _usuarios.ObterPorUsername(username.Trim())
            ?? throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        if (!usuario.Ativo)
            throw new UnauthorizedAccessException("Usuário inativo. Contate o administrador.");

        bool senhaOk;
        try
        {
            senhaOk = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Falha ao validar a senha (BCrypt). Verifique se o hash do usuário é válido.", ex);
        }

        if (!senhaOk)
            throw new UnauthorizedAccessException("Usuário ou senha inválidos.");

        _usuarios.RegistrarLogin(usuario.Id);
        usuario.UltimoLogin = DateTime.Now;
        return usuario;
    }
}
