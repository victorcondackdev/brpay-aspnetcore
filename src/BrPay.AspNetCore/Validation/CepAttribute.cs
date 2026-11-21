using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that a string member contains a Brazilian postal code (CEP) in the form
/// <c>00000-000</c> or <c>00000000</c>. Only the shape is checked; the CEP is not looked up.
/// <see langword="null"/> is considered valid; combine with <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CepAttribute : ValidationAttribute
{
    /// <summary>The default pt-BR error message. <c>{0}</c> is the member display name.</summary>
    public const string DefaultErrorMessage = "O campo {0} deve conter um CEP válido (00000-000).";

    /// <summary>Creates the attribute with the default pt-BR error message.</summary>
    public CepAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string s => IsValidCep(s),
        _ => false,
    };

    /// <summary>Whether <paramref name="s"/> has the shape of a CEP: 8 digits, optionally with a hyphen after the fifth.</summary>
    /// <param name="s">The candidate CEP.</param>
    /// <returns><see langword="true"/> for <c>00000-000</c> or <c>00000000</c> shapes, excluding all zeros.</returns>
    public static bool IsValidCep(ReadOnlySpan<char> s)
    {
        s = s.Trim();
        if (s.Length == 9)
        {
            if (s[5] != '-')
            {
                return false;
            }

            return AllDigits(s.Slice(0, 5)) && AllDigits(s.Slice(6, 3)) && !(IsZeros(s.Slice(0, 5)) && IsZeros(s.Slice(6, 3)));
        }

        return s.Length == 8 && AllDigits(s) && !IsZeros(s);
    }

    private static bool AllDigits(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsZeros(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
        {
            if (c != '0')
            {
                return false;
            }
        }

        return true;
    }
}
