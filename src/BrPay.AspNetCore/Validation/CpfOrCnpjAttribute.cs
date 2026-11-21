using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that a string member contains either a valid CPF or a valid CNPJ ("CPF/CNPJ" fields
/// of payer and beneficiary forms). <see langword="null"/> is considered valid; combine with
/// <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CpfOrCnpjAttribute : ValidationAttribute
{
    /// <summary>The default pt-BR error message. <c>{0}</c> is the member display name.</summary>
    public const string DefaultErrorMessage = "O campo {0} deve conter um CPF ou CNPJ válido.";

    /// <summary>Creates the attribute with the default pt-BR error message.</summary>
    public CpfOrCnpjAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <summary>Whether alphanumeric CNPJs are accepted. Defaults to <see langword="true"/>.</summary>
    public bool AllowAlphanumeric { get; set; } = true;

    /// <inheritdoc />
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string s => Cpf.IsValid(s) || Cnpj.IsValid(s, AllowAlphanumeric),
        Cpf cpf => !cpf.IsEmpty,
        Cnpj cnpj => !cnpj.IsEmpty && (AllowAlphanumeric || !cnpj.IsAlphanumeric),
        _ => false,
    };
}
