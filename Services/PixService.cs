using System.Globalization;
using System.Text;

namespace ClientePagamentoSystem.Services;

/// <summary>
/// Gera o payload Pix "copia e cola" (padrão BR Code / EMV) com chave, valor e txid,
/// pronto para virar QR Code.
/// </summary>
public static class PixService
{
    private static string Emv(string id, string valor)
        => id + valor.Length.ToString("D2") + valor;

    private static string SomenteAlfanumerico(string s, int maxLen)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '.')
                sb.Append(c);
        var r = sb.ToString().Trim().ToUpperInvariant();
        return r.Length > maxLen ? r[..maxLen] : r;
    }

    private static ushort Crc16(string s)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.ASCII.GetBytes(s))
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
        }
        return crc;
    }

    /// <summary>
    /// Normaliza a chave Pix conforme o tipo:
    /// CPF/CNPJ → só dígitos; telefone → +55DDDNUMERO; e-mail/aleatória → minúscula/igual.
    /// </summary>
    public static string NormalizarChave(string chave)
    {
        chave = chave.Trim();
        if (chave.Contains('@')) return chave.ToLowerInvariant();          // e-mail
        if (chave.Contains('-') && chave.Length == 36) return chave.ToLowerInvariant(); // UUID/aleatória

        var digitos = new string(chave.Where(char.IsDigit).ToArray());
        if (digitos.Length is 11 or 14 && !chave.StartsWith('+'))
            return digitos;                                                // CPF ou CNPJ
        if (digitos.Length is 10 or 11)
            return "+55" + digitos;                                        // telefone BR
        if (chave.StartsWith('+') && digitos.Length is 12 or 13)
            return "+" + digitos;                                          // telefone com DDI
        return digitos.Length > 0 ? digitos : chave;
    }

    /// <summary>Monta o payload Pix. txid deve ser alfanumérico (até 25 chars).</summary>
    public static string GerarPayload(string chave, string nome, string cidade,
        decimal valor, string txid)
    {
        chave = NormalizarChave(chave);

        // txid: apenas letras e números, até 25 caracteres
        var tx = new string(txid.Where(char.IsLetterOrDigit).ToArray());
        if (tx.Length == 0) tx = "PAG";
        if (tx.Length > 25) tx = tx[..25];

        var merchantInfo = Emv("00", "br.gov.bcb.pix") + Emv("01", chave);

        var sb = new StringBuilder()
            .Append(Emv("00", "01"))
            .Append(Emv("01", "12"))               // QR dinâmico (valor embutido)
            .Append(Emv("26", merchantInfo))
            .Append(Emv("52", "0000"))
            .Append(Emv("53", "986"))
            .Append(Emv("54", valor.ToString("F2", CultureInfo.InvariantCulture)))
            .Append(Emv("58", "BR"))
            .Append(Emv("59", SomenteAlfanumerico(nome, 25)))
            .Append(Emv("60", SomenteAlfanumerico(cidade, 15)))
            .Append(Emv("62", Emv("05", tx)))
            .Append("6304");
        var payload = sb.ToString();
        return payload + Crc16(payload).ToString("X4");
    }
}
