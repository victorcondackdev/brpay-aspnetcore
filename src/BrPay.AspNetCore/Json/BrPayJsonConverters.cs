using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrPay.AspNetCore.Json;

/// <summary>
/// Reads a <see cref="Cpf"/> from a JSON string (punctuation allowed) and writes its 11 raw digits.
/// Applied automatically through <see cref="JsonConverterAttribute"/> on <see cref="Cpf"/>.
/// </summary>
public sealed class CpfJsonConverter : JsonConverter<Cpf>
{
    /// <inheritdoc />
    public override Cpf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = JsonConverterHelpers.ReadString(ref reader, "CPF");
        return Cpf.TryParse(text, CultureInfo.InvariantCulture, out var cpf)
            ? cpf
            : throw new JsonException("The JSON value is not a valid CPF.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Cpf value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        JsonConverterHelpers.Write(writer, value.IsEmpty, value.Value);
    }
}

/// <summary>
/// Reads a <see cref="Cnpj"/> from a JSON string (punctuation and lowercase allowed) and writes
/// its 14 raw, uppercase characters. Applied automatically through <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class CnpjJsonConverter : JsonConverter<Cnpj>
{
    /// <inheritdoc />
    public override Cnpj Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = JsonConverterHelpers.ReadString(ref reader, "CNPJ");
        return Cnpj.TryParse(text, CultureInfo.InvariantCulture, out var cnpj)
            ? cnpj
            : throw new JsonException("The JSON value is not a valid CNPJ.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Cnpj value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        JsonConverterHelpers.Write(writer, value.IsEmpty, value.Value);
    }
}

/// <summary>
/// Reads a <see cref="PixKey"/> from a JSON string and writes its canonical value.
/// Applied automatically through <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class PixKeyJsonConverter : JsonConverter<PixKey>
{
    /// <inheritdoc />
    public override PixKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = JsonConverterHelpers.ReadString(ref reader, "Pix key");
        return PixKey.TryParse(text, CultureInfo.InvariantCulture, out var key)
            ? key
            : throw new JsonException("The JSON value is not a valid Pix key.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PixKey value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        JsonConverterHelpers.Write(writer, value.IsEmpty, value.Value);
    }
}

internal static class JsonConverterHelpers
{
    public static string ReadString(ref Utf8JsonReader reader, string what)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a JSON string containing a {what}.");
        }

        return reader.GetString() ?? string.Empty;
    }

    // default(T) is written as null so that it never round-trips into a bogus value.
    public static void Write(Utf8JsonWriter writer, bool isEmpty, string value)
    {
        if (isEmpty)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
