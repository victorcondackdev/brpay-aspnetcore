using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that a string (or <see cref="Cpf"/>) member contains a valid CPF. Punctuation is
/// allowed. <see langword="null"/> is considered valid; combine with <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CpfAttribute : ValidationAttribute
{
    /// <summary>The default pt-BR error message. <c>{0}</c> is the member display name.</summary>
    public const string DefaultErrorMessage = "O campo {0} deve conter um CPF válido.";

    /// <summary>Creates the attribute with the default pt-BR error message.</summary>
    public CpfAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string s => Cpf.IsValid(s),
        Cpf cpf => !cpf.IsEmpty,
        _ => false,
    };
}
