using System.Text;

namespace BrPay.AspNetCore.Internal;

/// <summary>Reason a CPF or CNPJ was rejected.</summary>
internal enum DocumentError
{
    None,
    Length,
    Characters,
    Repeated,
    CheckDigit,
}

/// <summary>
/// CPF and CNPJ rules, ported from github.com/victorcondackdev/brpay/document.
/// This is a deliberately small internal copy; it will be replaced by the BrPay
/// NuGet package once that is published.
/// </summary>
/// <remarks>
/// Alphanumeric CNPJ (Receita Federal, July 2026): the first 12 characters may be
/// digits or uppercase letters A–Z, the two check digits are always numeric, and the
/// modulo-11 weights use the value <c>character − '0'</c> for every character, which
/// keeps purely numeric CNPJs backward compatible.
/// </remarks>
internal static class DocumentRules
{
    public const int CpfLength = 11;
    public const int CnpjLength = 14;

    /// <summary>
    /// Removes the usual punctuation (".", "-", "/", spaces) and uppercases ASCII letters.
    /// Any other character is kept so that validation can reject it.
    /// </summary>
    public static string Clean(ReadOnlySpan<char> s)
    {
        var builder = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch is >= 'a' and <= 'z')
            {
                builder.Append((char)(ch - 'a' + 'A'));
            }
            else if (ch is not ('.' or '-' or '/' or ' '))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    /// <summary>Validates an already cleaned CPF.</summary>
    public static DocumentError ValidateCleanCpf(string c)
    {
        if (c.Length != CpfLength)
        {
            return DocumentError.Length;
        }

        foreach (var ch in c)
        {
            if (!IsDigit(ch))
            {
                return DocumentError.Characters;
            }
        }

        if (AllSame(c))
        {
            return DocumentError.Repeated;
        }

        // The second digit is computed over the first ten characters. If the first check
        // digit is wrong the result is a mismatch either way, so this matches the Go code.
        if (c[9] - '0' != CpfDigit(c, 9) || c[10] - '0' != CpfDigit(c, 10))
        {
            return DocumentError.CheckDigit;
        }

        return DocumentError.None;
    }

    /// <summary>Validates an already cleaned CNPJ, numeric or alphanumeric.</summary>
    public static DocumentError ValidateCleanCnpj(string c, bool allowAlphanumeric = true)
    {
        if (c.Length != CnpjLength)
        {
            return DocumentError.Length;
        }

        for (var i = 0; i < 12; i++)
        {
            var ch = c[i];
            var ok = IsDigit(ch) || (allowAlphanumeric && ch is >= 'A' and <= 'Z');
            if (!ok)
            {
                return DocumentError.Characters;
            }
        }

        if (!IsDigit(c[12]) || !IsDigit(c[13]))
        {
            return DocumentError.Characters;
        }

        if (AllSame(c))
        {
            return DocumentError.Repeated;
        }

        if (c[12] - '0' != CnpjDigit(c, 12) || c[13] - '0' != CnpjDigit(c, 13))
        {
            return DocumentError.CheckDigit;
        }

        return DocumentError.None;
    }

    public static bool IsValidCpf(ReadOnlySpan<char> s) => ValidateCleanCpf(Clean(s)) == DocumentError.None;

    public static bool IsValidCnpj(ReadOnlySpan<char> s, bool allowAlphanumeric = true) =>
        ValidateCleanCnpj(Clean(s), allowAlphanumeric) == DocumentError.None;

    /// <summary>Whether a cleaned, valid CNPJ contains at least one letter.</summary>
    public static bool HasLetters(string cleanCnpj)
    {
        for (var i = 0; i < 12 && i < cleanCnpj.Length; i++)
        {
            if (cleanCnpj[i] is >= 'A' and <= 'Z')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Formats a cleaned, valid CPF as 000.000.000-00.</summary>
    public static string FormatCpf(string c) =>
        string.Concat(c.AsSpan(0, 3), ".", c.AsSpan(3, 3), ".") + string.Concat(c.AsSpan(6, 3), "-", c.AsSpan(9, 2));

    /// <summary>Formats a cleaned, valid CNPJ as AA.AAA.AAA/AAAA-00.</summary>
    public static string FormatCnpj(string c) =>
        string.Concat(c.AsSpan(0, 2), ".", c.AsSpan(2, 3), ".") +
        string.Concat(c.AsSpan(5, 3), "/", c.AsSpan(8, 4), "-") +
        c.Substring(12, 2);

    /// <summary>Masks a cleaned, valid CPF following the Pix DICT pattern: ***.456.789-**.</summary>
    public static string MaskCpf(string c) =>
        string.Concat("***.", c.AsSpan(3, 3), ".") + string.Concat(c.AsSpan(6, 3), "-**");

    /// <summary>Masks a cleaned, valid CNPJ: 12.***.***/0001-**.</summary>
    public static string MaskCnpj(string c) =>
        string.Concat(c.AsSpan(0, 2), ".***.***/", c.AsSpan(8, 4)) + "-**";

    private static int CpfDigit(string c, int count)
    {
        // Weights run from count + 1 down to 2.
        var sum = 0;
        var weight = count + 1;
        for (var i = 0; i < count; i++)
        {
            sum += (c[i] - '0') * (weight - i);
        }

        var r = sum % 11;
        return r < 2 ? 0 : 11 - r;
    }

    private static int CnpjDigit(string c, int count)
    {
        // Weights 2..9 cycling from the rightmost character of the base.
        var sum = 0;
        var weight = 2;
        for (var i = count - 1; i >= 0; i--)
        {
            sum += (c[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var r = sum % 11;
        return r < 2 ? 0 : 11 - r;
    }

    private static bool IsDigit(char ch) => ch is >= '0' and <= '9';

    private static bool AllSame(string s)
    {
        for (var i = 1; i < s.Length; i++)
        {
            if (s[i] != s[0])
            {
                return false;
            }
        }

        return true;
    }
}
