using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that a string (or <see cref="PixKey"/>) member contains a valid Pix key whose type
/// is in <see cref="AllowedTypes"/>. <see langword="null"/> is considered valid; combine with
/// <see cref="RequiredAttribute"/>.
/// </summary>
/// <example>
/// <code>
/// public sealed record Payout([property: PixKey(AllowedTypes = PixKeyTypes.Email | PixKeyTypes.Evp)] string Key);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PixKeyAttribute : ValidationAttribute
{
    /// <summary>The default pt-BR error message. <c>{0}</c> is the member display name.</summary>
    public const string DefaultErrorMessage = "O campo {0} deve conter uma chave Pix válida.";

    /// <summary>Creates the attribute with the default pt-BR error message.</summary>
    public PixKeyAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <summary>The key types accepted. Defaults to <see cref="PixKeyTypes.All"/>.</summary>
    public PixKeyTypes AllowedTypes { get; set; } = PixKeyTypes.All;

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        switch (value)
        {
            case null:
                return true;
            case string s:
                return PixKey.TryParse(s, out var parsed) && IsAllowed(parsed.Type);
            case PixKey key:
                return !key.IsEmpty && IsAllowed(key.Type);
            default:
                return false;
        }
    }

    private bool IsAllowed(PixKeyType type)
    {
        var flag = type.ToFlag();
        return flag != PixKeyTypes.None && (AllowedTypes & flag) == flag;
    }
}
