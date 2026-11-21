namespace BrPay.AspNetCore.Internal;

/// <summary>
/// Pix key ("chave Pix") detection and normalization, ported from
/// github.com/victorcondackdev/brpay/pix (key.go). Internal until the BrPay package
/// is published on NuGet.
/// </summary>
/// <remarks>
/// The type is decided by shape, in this order:
/// <list type="bullet">
/// <item>contains "@": e-mail, lowercased, at most 77 characters, ASCII only, dotted domain;</item>
/// <item>starts with "+": phone; spaces, hyphens, dots and parentheses are removed; it must be
/// +55, a two-digit area code without zeros and 8 digits, or 9 digits starting with 9;</item>
/// <item>UUID shape (36 characters, hyphens at 8, 13, 18, 23): EVP, lowercased;</item>
/// <item>otherwise, after removing punctuation: 11 characters is a CPF and 14 a CNPJ
/// (alphanumeric allowed), both with valid check digits.</item>
/// </list>
/// National phone numbers without "+55" are rejected because an 11-digit string is
/// indistinguishable from a CPF.
/// </remarks>
internal static class PixKeyRules
{
    public const int MaxEmailKeyLength = 77;

    private const string EmailLocalSymbols = ".!#$%&'*+/=?^_`{|}~-";

    /// <summary>Validates and normalizes a Pix key.</summary>
    /// <returns><see langword="null"/> on success, otherwise the reason for rejection.</returns>
    public static string? TryNormalize(ReadOnlySpan<char> input, out string normalized, out PixKeyType type)
    {
        normalized = string.Empty;
        type = PixKeyType.Unknown;

        var s = input.Trim();
        if (s.IsEmpty)
        {
            return "empty";
        }

        string? error;
        if (s.IndexOf('@') >= 0)
        {
            error = NormalizeEmail(s, out normalized);
            return Finish(error, PixKeyType.Email, ref normalized, ref type);
        }

        if (s[0] == '+')
        {
            error = NormalizePhone(s, out normalized);
            return Finish(error, PixKeyType.Phone, ref normalized, ref type);
        }

        if (IsUuidShape(s))
        {
            error = NormalizeEvp(s, out normalized);
            return Finish(error, PixKeyType.Evp, ref normalized, ref type);
        }

        var clean = DocumentRules.Clean(s);
        switch (clean.Length)
        {
            case DocumentRules.CpfLength:
                if (DocumentRules.ValidateCleanCpf(clean) != DocumentError.None)
                {
                    return "invalid CPF (phone keys must start with +55)";
                }

                normalized = clean;
                type = PixKeyType.Cpf;
                return null;

            case DocumentRules.CnpjLength:
                if (DocumentRules.ValidateCleanCnpj(clean) != DocumentError.None)
                {
                    return "invalid CNPJ";
                }

                normalized = clean;
                type = PixKeyType.Cnpj;
                return null;

            default:
                return "not a CPF, CNPJ, e-mail, phone (+55...) or EVP";
        }
    }

    /// <summary>Masks a normalized key for logs.</summary>
    public static string Mask(string value, PixKeyType type)
    {
        switch (type)
        {
            case PixKeyType.Cpf:
                return DocumentRules.MaskCpf(value);
            case PixKeyType.Cnpj:
                return DocumentRules.MaskCnpj(value);
            case PixKeyType.Email:
            {
                var at = value.IndexOf('@');
                return string.Concat(value.AsSpan(0, 1), "***", value.AsSpan(at));
            }

            case PixKeyType.Phone:
                // +55 DD ***** NNNN
                return string.Concat(value.AsSpan(0, 5), "*****", value.AsSpan(value.Length - 4));
            case PixKeyType.Evp:
                return string.Concat(value.AsSpan(0, 8), "-****-****-****-********", value.AsSpan(32));
            default:
                return string.Empty;
        }
    }

    /// <summary>Formats a normalized key for display: documents are punctuated, the rest is unchanged.</summary>
    public static string Format(string value, PixKeyType type) => type switch
    {
        PixKeyType.Cpf => DocumentRules.FormatCpf(value),
        PixKeyType.Cnpj => DocumentRules.FormatCnpj(value),
        _ => value,
    };

    private static string? Finish(string? error, PixKeyType success, ref string normalized, ref PixKeyType type)
    {
        if (error is not null)
        {
            normalized = string.Empty;
            type = PixKeyType.Unknown;
            return error;
        }

        type = success;
        return null;
    }

    private static string? NormalizeEmail(ReadOnlySpan<char> s, out string normalized)
    {
        normalized = string.Empty;
        if (s.Length > MaxEmailKeyLength)
        {
            return "e-mail longer than 77 characters";
        }

        var e = new char[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            e[i] = (c is >= 'A' and <= 'Z') ? (char)(c + ('a' - 'A')) : c;
        }

        var at = s.IndexOf('@');
        if (at != s.LastIndexOf('@'))
        {
            return "e-mail has more than one @";
        }

        var local = e.AsSpan(0, at);
        var domain = e.AsSpan(at + 1);
        if (local.Length is 0 or > 64)
        {
            return "e-mail local part must have 1 to 64 characters";
        }

        if (local[0] == '.' || local[^1] == '.' || local.IndexOf("..".AsSpan()) >= 0)
        {
            return "misplaced dot in e-mail local part";
        }

        foreach (var c in local)
        {
            if (!IsAlnumLower(c) && EmailLocalSymbols.IndexOf(c) < 0)
            {
                return "invalid character in e-mail";
            }
        }

        if (domain.IndexOf('.') < 0)
        {
            return "e-mail domain must contain a dot";
        }

        var rest = domain;
        while (true)
        {
            var dot = rest.IndexOf('.');
            var label = dot < 0 ? rest : rest.Slice(0, dot);
            if (label.IsEmpty || label.Length > 63 || label[0] == '-' || label[^1] == '-')
            {
                return "invalid e-mail domain";
            }

            foreach (var c in label)
            {
                if (!IsAlnumLower(c) && c != '-')
                {
                    return "invalid character in e-mail domain";
                }
            }

            if (dot < 0)
            {
                break;
            }

            rest = rest.Slice(dot + 1);
        }

        normalized = new string(e);
        return null;
    }

    private static string? NormalizePhone(ReadOnlySpan<char> s, out string normalized)
    {
        normalized = string.Empty;
        Span<char> digits = stackalloc char[16];
        var n = 0;
        for (var i = 1; i < s.Length; i++)
        {
            var c = s[i];
            if (c is >= '0' and <= '9')
            {
                if (n == digits.Length)
                {
                    return "phone number too long";
                }

                digits[n++] = c;
            }
            else if (c is not (' ' or '-' or '.' or '(' or ')'))
            {
                return "invalid character in phone number";
            }
        }

        var d = digits.Slice(0, n);
        if (n < 2 || d[0] != '5' || d[1] != '5')
        {
            return "phone keys must be Brazilian numbers (+55)";
        }

        var national = d.Slice(2);
        if (national.Length is not (10 or 11))
        {
            return "phone number must have a 2-digit area code and 8 or 9 digits";
        }

        if (national[0] == '0' || national[1] == '0')
        {
            return "invalid area code (DDD)";
        }

        if (national.Length == 11 && national[2] != '9')
        {
            return "9-digit phone numbers must start with 9";
        }

        normalized = "+" + new string(d);
        return null;
    }

    private static bool IsUuidShape(ReadOnlySpan<char> s) =>
        s.Length == 36 && s[8] == '-' && s[13] == '-' && s[18] == '-' && s[23] == '-';

    private static string? NormalizeEvp(ReadOnlySpan<char> s, out string normalized)
    {
        normalized = string.Empty;
        var b = new char[s.Length];
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i is 8 or 13 or 18 or 23)
            {
                b[i] = c;
                continue;
            }

            if (c is (>= '0' and <= '9') or (>= 'a' and <= 'f'))
            {
                b[i] = c;
            }
            else if (c is >= 'A' and <= 'F')
            {
                b[i] = (char)(c + ('a' - 'A'));
            }
            else
            {
                return "EVP must be a hexadecimal UUID";
            }
        }

        normalized = new string(b);
        return null;
    }

    private static bool IsAlnumLower(char c) => c is (>= 'a' and <= 'z') or (>= '0' and <= '9');
}
