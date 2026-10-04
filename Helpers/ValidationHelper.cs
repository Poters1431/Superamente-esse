using System.Text.RegularExpressions;

namespace ClientePagamentoSystem.Helpers;

public static class ValidationHelper
{
    public static string SomenteNumeros(string? texto)
        => texto is null ? string.Empty : Regex.Replace(texto, @"\D", "");

    public static bool CpfValido(string? cpf)
    {
        var numeros = SomenteNumeros(cpf);
        if (numeros.Length != 11) return false;
        if (numeros.Distinct().Count() == 1) return false; // 111.111.111-11 etc.

        int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var base9 = numeros[..9];
        int soma = base9.Select((c, i) => (c - '0') * mult1[i]).Sum();
        int digito1 = (soma * 10 % 11) % 10;

        var base10 = base9 + digito1;
        soma = base10.Select((c, i) => (c - '0') * mult2[i]).Sum();
        int digito2 = (soma * 10 % 11) % 10;

        return numeros.EndsWith($"{digito1}{digito2}");
    }

    public static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email.Trim(),
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }

    public static string FormatarCpf(string cpf)
    {
        var n = SomenteNumeros(cpf);
        return n.Length == 11
            ? Convert.ToUInt64(n).ToString(@"000\.000\.000\-00")
            : cpf;
    }
}
