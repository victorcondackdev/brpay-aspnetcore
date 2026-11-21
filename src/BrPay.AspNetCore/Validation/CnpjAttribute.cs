using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that a string (or <see cref="Cnpj"/>) member contains a valid CNPJ. Punctuation is
/// allowed; the alphanumeric format is accepted unless <see cref="AllowAlphanumeric"/> is
/// <see langword="false"/>. <see langword="null"/> is considered valid; combine with
/// <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CnpjAttribute : ValidationAttribute
{
    /// <summary>The default pt-BR error message. <c>{0}</c> is the member display name.</summary>
    public const string DefaultErrorMessage = "O campo {0} deve conter um CNPJ válido.";

    /// <summary>Creates the attribute with the default pt-BR error message.</summary>
    public CnpjAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <summary>
    /// Whether letters A–Z are accepted in the first 12 characters (alphanumeric CNPJ, issued
    /// from July 2026). Defaults to <see langword="true"/>.
    /// </summary>
    public bool AllowAlphanumeric { get; set; } = true;

    /// <inheritdoc />
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string s => Cnpj.IsValid(s, AllowAlphanumeric),
        Cnpj cnpj => !cnpj.IsEmpty && (AllowAlphanumeric || !cnpj.IsAlphanumeric),
        _ => false,
    };
}
