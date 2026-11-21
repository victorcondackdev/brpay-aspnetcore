using System.Text.Json;

namespace BrPay.AspNetCore;

/// <summary>Options for the BrPay ASP.NET Core integration, configured with <see cref="Microsoft.Extensions.DependencyInjection.BrPayServiceCollectionExtensions.AddBrPay"/>.</summary>
public sealed class BrPayOptions
{
    /// <summary>The <c>title</c> of the validation problem details. Defaults to a pt-BR message.</summary>
    public string ValidationProblemTitle { get; set; } = "Um ou mais campos são inválidos.";

    /// <summary>
    /// The naming policy applied to member names used as keys of the <c>errors</c> dictionary,
    /// so they match the JSON property names clients sent. Defaults to
    /// <see cref="JsonNamingPolicy.CamelCase"/>; set to <see langword="null"/> to keep CLR names.
    /// A <c>[JsonPropertyName]</c> on the member always wins.
    /// </summary>
    public JsonNamingPolicy? ErrorKeyNamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;
}
