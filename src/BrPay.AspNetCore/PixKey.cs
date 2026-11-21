using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BrPay.AspNetCore.Internal;
using BrPay.AspNetCore.Json;

namespace BrPay.AspNetCore;

/// <summary>
/// A validated Pix key ("chave Pix") in the canonical form used by the DICT and inside BR Codes:
/// CPF and CNPJ without punctuation, lowercase e-mail, phone in E.164 (<c>+5521987654321</c>)
/// or lowercase EVP UUID. Binds directly from route and query parameters in minimal APIs and
/// serializes as its canonical value with System.Text.Json.
/// </summary>
/// <remarks>
/// LGPD: <see cref="ToString()"/> returns a masked form (<c>***.982.247-**</c>,
/// <c>f***@example.com</c>, <c>+5521*****4321</c>). Supported formats for
/// <see cref="ToString(string, IFormatProvider)"/>: <c>"M"</c> (masked, the default),
/// <c>"F"</c> (display form; documents are punctuated) and <c>"V"</c> (canonical value).
/// </remarks>
[JsonConverter(typeof(PixKeyJsonConverter))]
public readonly record struct PixKey : ISpanParsable<PixKey>, IFormattable
{
    private readonly string? _value;

    private PixKey(string value, PixKeyType type)
    {
        _value = value;
        Type = type;
    }

    /// <summary>The canonical key, or an empty string for <c>default(PixKey)</c>.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>The key type; <see cref="PixKeyType.Unknown"/> for <c>default(PixKey)</c>.</summary>
    public PixKeyType Type { get; }

    /// <summary>Whether this is the uninitialized <c>default(PixKey)</c>.</summary>
    public bool IsEmpty => _value is null;

    /// <summary>The display form: CPF and CNPJ punctuated, other types unchanged.</summary>
    public string Formatted => _value is null ? string.Empty : PixKeyRules.Format(_value, Type);

    /// <summary>The masked form used in logs.</summary>
    public string Masked => _value is null ? string.Empty : PixKeyRules.Mask(_value, Type);

    /// <summary>Whether <paramref name="s"/> is a valid Pix key.</summary>
    /// <param name="s">The candidate key.</param>
    /// <returns><see langword="true"/> when the key is valid.</returns>
    public static bool IsValid(ReadOnlySpan<char> s) => PixKeyRules.TryNormalize(s, out _, out _) is null;

    /// <summary>Detects the type of a Pix key.</summary>
    /// <param name="s">The candidate key.</param>
    /// <returns>The key type, or <see cref="PixKeyType.Unknown"/> when the key is invalid.</returns>
    public static PixKeyType Detect(ReadOnlySpan<char> s)
    {
        _ = PixKeyRules.TryNormalize(s, out _, out var type);
        return type;
    }

    /// <summary>Parses a Pix key.</summary>
    /// <param name="s">The text to parse.</param>
    /// <returns>The parsed key.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid Pix key; the message says why.</exception>
    public static PixKey Parse(string s) => Parse(s, provider: null);

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; Pix keys are culture invariant.</param>
    public static PixKey Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; Pix keys are culture invariant.</param>
    public static PixKey Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        var error = PixKeyRules.TryNormalize(s, out var value, out var type);
        if (error is not null)
        {
            throw new FormatException("Invalid Pix key: " + error + ".");
        }

        return new PixKey(value, type);
    }

    /// <summary>Tries to parse a Pix key.</summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">The parsed key, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when <paramref name="s"/> is a valid Pix key.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out PixKey result) => TryParse(s, provider: null, out result);

    /// <inheritdoc cref="TryParse(string, out PixKey)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; Pix keys are culture invariant.</param>
    /// <param name="result">The parsed key, or <c>default</c> on failure.</param>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PixKey result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="TryParse(string, out PixKey)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; Pix keys are culture invariant.</param>
    /// <param name="result">The parsed key, or <c>default</c> on failure.</param>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PixKey result)
    {
        if (PixKeyRules.TryNormalize(s, out var value, out var type) is not null)
        {
            result = default;
            return false;
        }

        result = new PixKey(value, type);
        return true;
    }

    /// <summary>Returns the masked form. Use <see cref="Value"/> for the canonical key.</summary>
    /// <returns>The masked key.</returns>
    public override string ToString() => Masked;

    /// <summary>Formats the key: <c>"M"</c> masked (default), <c>"F"</c> display form, <c>"V"</c> canonical value.</summary>
    /// <param name="format">The format, or <see langword="null"/> for <c>"M"</c>.</param>
    /// <param name="formatProvider">Ignored.</param>
    /// <returns>The formatted key.</returns>
    /// <exception cref="FormatException">The format is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ValueFormat.Parse(format) switch
    {
        ValueFormat.Kind.Formatted => Formatted,
        ValueFormat.Kind.Raw => Value,
        _ => Masked,
    };
}
