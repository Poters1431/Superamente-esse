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

    /// <summary>Monta o payload Pix. txid deve ser alfanumérico (até 25 chars).</summary>
    public static string GerarPayload(string chave, string nome, string cidade,
        decimal valor, string txid)
    {
        var merchantInfo = Emv("00", "br.gov.bcb.pix") + Emv("01", chave.Trim());
        var payload = new StringBuilder()
            .Append(Emv("00", "01"))
            .Append(Emv("26", merchantInfo))
            .Append(Emv("52", "0000"))
            .Append(Emv("53", "986"))
            .Append(Emv("54", valor.ToString("F2", CultureInfo.InvariantCulture)))
            .Append(Emv("58", "BR"))
            .Append(Emv("59", SomenteAlfanumerico(nome, 25)))
            .Append(Emv("60", SomenteAlfanumerico(cidade, 15)))
            .Append(Emv("62", Emv("05", txid.Length > 25 ? txid[..25] : txid)))
            .Append("6304")
            .ToString();

        return payload + Crc16(payload).ToString("X4");
    }
}
