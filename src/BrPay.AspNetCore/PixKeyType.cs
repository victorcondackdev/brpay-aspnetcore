using System.Text.Json.Serialization;

namespace BrPay.AspNetCore;

/// <summary>The type of a Pix key ("chave Pix") as registered in the DICT.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PixKeyType>))]
public enum PixKeyType
{
    /// <summary>Not a valid Pix key.</summary>
    Unknown = 0,

    /// <summary>11-digit CPF.</summary>
    Cpf = 1,

    /// <summary>14-character CNPJ, numeric or alphanumeric.</summary>
    Cnpj = 2,

    /// <summary>E-mail address, lowercased, at most 77 characters.</summary>
    Email = 3,

    /// <summary>Brazilian phone number in E.164 format, for example <c>+5521987654321</c>.</summary>
    Phone = 4,

    /// <summary>Random key ("chave aleatória"): lowercase UUID with hyphens.</summary>
    Evp = 5,
}

/// <summary>A set of <see cref="PixKeyType"/> values, used to restrict which keys an endpoint accepts.</summary>
[Flags]
public enum PixKeyTypes
{
    /// <summary>No key type.</summary>
    None = 0,

    /// <summary>CPF keys.</summary>
    Cpf = 1 << 0,

    /// <summary>CNPJ keys.</summary>
    Cnpj = 1 << 1,

    /// <summary>E-mail keys.</summary>
    Email = 1 << 2,

    /// <summary>Phone keys.</summary>
    Phone = 1 << 3,

    /// <summary>Random (EVP) keys.</summary>
    Evp = 1 << 4,

    /// <summary>CPF or CNPJ keys.</summary>
    Document = Cpf | Cnpj,

    /// <summary>Every key type.</summary>
    All = Cpf | Cnpj | Email | Phone | Evp,
}

/// <summary>Helpers for <see cref="PixKeyType"/>.</summary>
public static class PixKeyTypeExtensions
{
    /// <summary>Converts a single key type into its flag in <see cref="PixKeyTypes"/>.</summary>
    /// <param name="type">The key type.</param>
    /// <returns>The matching flag, or <see cref="PixKeyTypes.None"/> for <see cref="PixKeyType.Unknown"/>.</returns>
    public static PixKeyTypes ToFlag(this PixKeyType type) => type switch
    {
        PixKeyType.Cpf => PixKeyTypes.Cpf,
        PixKeyType.Cnpj => PixKeyTypes.Cnpj,
        PixKeyType.Email => PixKeyTypes.Email,
        PixKeyType.Phone => PixKeyTypes.Phone,
        PixKeyType.Evp => PixKeyTypes.Evp,
        _ => PixKeyTypes.None,
    };

    /// <summary>Returns the DICT name of the key type: CPF, CNPJ, EMAIL, PHONE, EVP or UNKNOWN.</summary>
    /// <param name="type">The key type.</param>
    /// <returns>The DICT name.</returns>
    public static string ToDictName(this PixKeyType type) => type switch
    {
        PixKeyType.Cpf => "CPF",
        PixKeyType.Cnpj => "CNPJ",
        PixKeyType.Email => "EMAIL",
        PixKeyType.Phone => "PHONE",
        PixKeyType.Evp => "EVP",
        _ => "UNKNOWN",
    };
}
