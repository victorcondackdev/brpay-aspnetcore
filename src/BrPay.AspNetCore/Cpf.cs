using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BrPay.AspNetCore.Internal;
using BrPay.AspNetCore.Json;

namespace BrPay.AspNetCore;

/// <summary>
/// A validated CPF (Cadastro de Pessoas Físicas). Binds directly from route and query
/// parameters in minimal APIs (through <see cref="TryParse(string, IFormatProvider, out Cpf)"/>)
/// and serializes as its 11 raw digits with System.Text.Json.
/// </summary>
/// <remarks>
/// LGPD: <see cref="ToString()"/> returns the masked form (<c>***.982.247-**</c>), so the value
/// is safe to interpolate into log messages and exception texts. Read <see cref="Value"/>
/// explicitly when the raw digits are required.
/// Supported formats for <see cref="ToString(string, IFormatProvider)"/>: <c>"M"</c> (masked,
/// the default), <c>"F"</c> (formatted, <c>529.982.247-25</c>) and <c>"V"</c> (raw digits).
/// </remarks>
[JsonConverter(typeof(CpfJsonConverter))]
public readonly record struct Cpf : ISpanParsable<Cpf>, IFormattable
{
    private readonly string? _value;

    private Cpf(string value) => _value = value;

    /// <summary>The 11 raw digits, or an empty string for <c>default(Cpf)</c>.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>Whether this is the uninitialized <c>default(Cpf)</c>.</summary>
    public bool IsEmpty => _value is null;

    /// <summary>The punctuated form, <c>000.000.000-00</c>.</summary>
    public string Formatted => _value is null ? string.Empty : DocumentRules.FormatCpf(_value);

    /// <summary>The masked form used in logs, <c>***.000.000-**</c>.</summary>
    public string Masked => _value is null ? string.Empty : DocumentRules.MaskCpf(_value);

    /// <summary>Whether <paramref name="s"/> is a valid CPF. Punctuation is allowed.</summary>
    /// <param name="s">The candidate CPF.</param>
    /// <returns><see langword="true"/> when the check digits are valid.</returns>
    public static bool IsValid(ReadOnlySpan<char> s) => DocumentRules.IsValidCpf(s);

    /// <summary>Parses a CPF, with or without punctuation.</summary>
    /// <param name="s">The text to parse.</param>
    /// <returns>The parsed CPF.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid CPF.</exception>
    public static Cpf Parse(string s) => Parse(s, provider: null);

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CPFs are culture invariant.</param>
    public static Cpf Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <inheritdoc cref="Parse(string)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CPFs are culture invariant.</param>
    public static Cpf Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException("The value is not a valid CPF.");

    /// <summary>Tries to parse a CPF, with or without punctuation.</summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">The parsed CPF, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when <paramref name="s"/> is a valid CPF.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out Cpf result) => TryParse(s, provider: null, out result);

    /// <inheritdoc cref="TryParse(string, out Cpf)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CPFs are culture invariant.</param>
    /// <param name="result">The parsed CPF, or <c>default</c> on failure.</param>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Cpf result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="TryParse(string, out Cpf)"/>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">Ignored; CPFs are culture invariant.</param>
    /// <param name="result">The parsed CPF, or <c>default</c> on failure.</param>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Cpf result)
    {
        var clean = DocumentRules.Clean(s);
        if (DocumentRules.ValidateCleanCpf(clean) != DocumentError.None)
        {
            result = default;
            return false;
        }

        result = new Cpf(clean);
        return true;
    }

    /// <summary>Returns the masked form (<c>***.982.247-**</c>). Use <see cref="Value"/> for the raw digits.</summary>
    /// <returns>The masked CPF.</returns>
    public override string ToString() => Masked;

    /// <summary>Formats the CPF: <c>"M"</c> masked (default), <c>"F"</c> punctuated, <c>"V"</c> raw digits.</summary>
    /// <param name="format">The format, or <see langword="null"/> for <c>"M"</c>.</param>
    /// <param name="formatProvider">Ignored.</param>
    /// <returns>The formatted CPF.</returns>
    /// <exception cref="FormatException">The format is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) => ValueFormat.Parse(format) switch
    {
        ValueFormat.Kind.Formatted => Formatted,
        ValueFormat.Kind.Raw => Value,
        _ => Masked,
    };
}
