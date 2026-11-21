using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BrPay.AspNetCore;

/// <summary>
/// Validates that an integral member holds an amount in centavos (BRL minor units) between
/// <see cref="Min"/> and <see cref="Max"/>, inclusive. Money is carried as integers to avoid
/// floating point rounding; <see cref="decimal"/>, <see cref="double"/> and <see cref="float"/>
/// values are rejected on purpose. <see langword="null"/> is considered valid.
/// </summary>
/// <example>
/// <code>
/// public sealed record Charge([property: BrlCents(Min = 100, Max = 1_000_000)] long AmountCents);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class BrlCentsAttribute : ValidationAttribute
{
    /// <summary>
    /// The default pt-BR error message. <c>{0}</c> is the member display name, <c>{1}</c> and
    /// <c>{2}</c> are <see cref="Min"/> and <see cref="Max"/> formatted as reais.
    /// </summary>
    public const string DefaultErrorMessage = "O campo {0} deve ser um valor em centavos entre {1} e {2}.";

    private static readonly NumberFormatInfo BrlNumberFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NegativeSign = "-",
    };

    /// <summary>Creates the attribute accepting 1 centavo up to <see cref="long.MaxValue"/>.</summary>
    public BrlCentsAttribute()
        : base(DefaultErrorMessage)
    {
    }

    /// <summary>Creates the attribute with explicit bounds.</summary>
    /// <param name="min">The minimum amount in centavos, inclusive.</param>
    /// <param name="max">The maximum amount in centavos, inclusive.</param>
    public BrlCentsAttribute(long min, long max)
        : base(DefaultErrorMessage)
    {
        Min = min;
        Max = max;
    }

    /// <summary>The minimum amount in centavos, inclusive. Defaults to 1 (zero is not a payment).</summary>
    public long Min { get; set; } = 1;

    /// <summary>The maximum amount in centavos, inclusive. Defaults to <see cref="long.MaxValue"/>.</summary>
    public long Max { get; set; } = long.MaxValue;

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        long cents;
        switch (value)
        {
            case null:
                return true;
            case long l:
                cents = l;
                break;
            case int i:
                cents = i;
                break;
            case short s:
                cents = s;
                break;
            case byte b:
                cents = b;
                break;
            case uint ui:
                cents = ui;
                break;
            case ulong ul when ul <= long.MaxValue:
                cents = (long)ul;
                break;
            default:
                return false;
        }

        return cents >= Min && cents <= Max;
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, FormatReais(Min), FormatReais(Max));

    /// <summary>Formats an amount in centavos as reais, for example <c>R$ 1.234,56</c>.</summary>
    /// <param name="cents">The amount in centavos.</param>
    /// <returns>The amount in the pt-BR currency format, independent of the current culture.</returns>
    public static string FormatReais(long cents) =>
        "R$ " + (cents / 100m).ToString("#,0.00", BrlNumberFormat);
}
