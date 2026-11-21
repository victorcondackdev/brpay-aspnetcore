using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BrPay.AspNetCore.Internal;
using BrPay.AspNetCore.Json;

namespace BrPay.AspNetCore;

/// <summary>
/// A validated CNPJ (Cadastro Nacional da Pessoa Jurídica), numeric or alphanumeric. Binds
/// directly from route and query parameters in minimal APIs and serializes as its 14 raw,
/// uppercase characters with System.Text.Json.
/// </summary>
/// <remarks>
/// Since July 2026 the Receita Federal issues alphanumeric CNPJs: the first 12 characters may be
/// digits or letters A–Z and the last two are numeric check digits. Lowercase input is accepted
/// and normalized to uppercase.
/// LGPD: <see cref="ToString()"/> returns the masked form (<c>12.***.***/0001-**</c>). Supported
/// formats for <see cref="ToString(string, IFormatProvider)"/>: <c>"M"</c> (masked, the default),
/// <c>"F"</c> (formatted, <c>12.ABC.345/01DE-35</c>) and <c>"V"</c> (raw characters).
/// </remarks>
[JsonConverter(typeof(CnpjJsonConverter))]
public readonly record struct Cnpj : ISpanParsable<Cnpj>, IFormattable
{
    private readonly string? _value;

    private Cnpj(string value) => _value = value;

    /// <summary>The 14 raw characters (uppercase), or an empty string for <c>default(Cnpj)</c>.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>Whether this is the uninitialized <c>default(Cnpj)</c>.</summary>
    public bool IsEmpty => _value is null;

    /// <summary>Whether the CNPJ uses the alphanumeric format (contains at least one letter).</summary>
    public bool IsAlphanumeric => _value is not null && DocumentRules.HasLetters(_value);

    /// <summary>The punctuated form, <c>00.000.000/0000-00</c>.</summary>
    public string Formatted => _value is null ? string.Empty : DocumentRules.FormatCnpj(_value);

    /// <summary>The masked form used in logs, <c>00.***.***/0000-**</c>.</summary>
    public string Masked => _value is null ? string.Empty : DocumentRules.MaskCnpj(_value);

    /// <summary>Whether <paramref name="s"/> is a valid CNPJ. Punctuation is allowed.</summary>
    /// <param name="s">The candidate CNPJ.</param>
    /// <param name="allowAlphanumeric">Whether letters are accepted in the first 12 characters.</param>
    /// <returns><see langword="true"/> when the check digits are valid.</returns>
    public static bool IsValid(ReadOnlySpan<char> s, bool allowAlphanumeric = true) =>
        DocumentRules.IsValidCnpj(s, allowAlphanumeric);

    /// <summary>Parses a CNPJ, with or without punctuation.</summary>
    /// <param name="s">The text to parse.</param>
    /// <returns>The parsed CNPJ.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid CNPJ.</exception>
    public static Cnpj Parse(string s) => Parse(s, provider: null);

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CNPJs are culture invariant.</param>
    public static Cnpj Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CNPJs are culture invariant.</param>
    public static Cnpj Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException("The value is not a valid CNPJ.");

    /// <summary>Tries to parse a CNPJ, with or without punctuation.</summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">The parsed CNPJ, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when <paramref name="s"/> is a valid CNPJ.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out Cnpj result) => TryParse(s, provider: null, out result);

    /// <inheritdoc cref="TryParse(string, out Cnpj)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CNPJs are culture invariant.</param>
    /// <param name="result">The parsed CNPJ, or <c>default</c> on failure.</param>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Cnpj result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="TryParse(string, out Cnpj)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CNPJs are culture invariant.</param>
    /// <param name="result">The parsed CNPJ, or <c>default</c> on failure.</param>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cnpj result)
    {
        var clean = DocumentRules.Clean(s);
        if (DocumentRules.ValidateCleanCnpj(clean) != DocumentError.None)
        {
            result = default;
            return false;
        }

        result = new Cnpj(clean);
        return true;
    }

    /// <summary>Returns the masked form (<c>12.***.***/0001-**</c>). Use <see cref="Value"/> for the raw characters.</summary>
    /// <returns>The masked CNPJ.</returns>
    public override string ToString() => Masked;

    /// <summary>Formats the CNPJ: <c>"M"</c> masked (default), <c>"F"</c> punctuated, <c>"V"</c> raw characters.</summary>
    /// <param name="format">The format, or <see langword="null"/> for <c>"M"</c>.</param>
    /// <param name="formatProvider">Ignored.</param>
    /// <returns>The formatted CNPJ.</returns>
    /// <exception cref="FormatException">The format is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ValueFormat.Parse(format) switch
    {
        ValueFormat.Kind.Formatted => Formatted,
        ValueFormat.Kind.Raw => Value,
        _ => Masked,
    };
}
