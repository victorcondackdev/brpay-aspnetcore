namespace BrPay.AspNetCore.Internal;

/// <summary>Shared format-string handling for the value objects.</summary>
internal static class ValueFormat
{
    internal enum Kind
    {
        Masked,
        Formatted,
        Raw,
    }

    public static Kind Parse(string? format) => format switch
    {
        null or "" or "M" or "m" or "G" or "g" => Kind.Masked,
        "F" or "f" => Kind.Formatted,
        "V" or "v" => Kind.Raw,
        _ => throw new FormatException($"Unsupported format '{format}'. Use \"M\" (masked), \"F\" (formatted) or \"V\" (raw)."),
    };
}
