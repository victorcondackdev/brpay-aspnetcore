using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BrPay.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddBrPay(options => options.ValidationProblemTitle = "Dados do pagador inválidos.");

var app = builder.Build();

app.UseStatusCodePages();

// DataAnnotations on a positional record, validated by the endpoint filter.
// Invalid input returns 400 application/problem+json with an "errors" dictionary.
app.MapPost("/payers", (CreatePayer payer, ILogger<Program> logger) =>
    {
        var document = Cpf.Parse(payer.Document);

        // Cpf.ToString() is masked, so this is safe to log under LGPD: "***.982.247-**".
        logger.LogInformation("Payer {Document} registered", document);

        return Results.Created($"/payers/{document.Value}", new PayerCreated(document.Masked, payer.Name));
    })
    .WithBrazilianDocumentValidation();

// Value objects bind straight from the route through TryParse; invalid values yield 400.
app.MapGet("/payers/{cpf}", (Cpf cpf) => Results.Ok(new PayerView(cpf.Masked, cpf.Formatted)));

app.MapGet("/keys/{key}", (PixKey key) =>
    Results.Ok(new PixKeyView(key.Type, key.ToString("M", CultureInfo.InvariantCulture))));

// Parameter-level attributes work too.
app.MapGet("/companies", ([Cnpj] string cnpj) => Results.Ok(new { cnpj = Cnpj.Parse(cnpj).Formatted }))
    .WithBrazilianDocumentValidation();

app.Run();

/// <summary>Request body for POST /payers.</summary>
public sealed record CreatePayer(
    [Required, Cpf] string Document,
    [Required, StringLength(120)] string Name,
    [PixKey(AllowedTypes = PixKeyTypes.Email | PixKeyTypes.Phone | PixKeyTypes.Evp)] string? Key,
    [BrlCents(Min = 100, Max = 10_000_000)] long AmountCents,
    [Cep] string? PostalCode);

/// <summary>Response body for POST /payers.</summary>
public sealed record PayerCreated(string Document, string Name);

/// <summary>Response body for GET /payers/{cpf}.</summary>
public sealed record PayerView(string Masked, string Formatted);

/// <summary>Response body for GET /keys/{key}.</summary>
public sealed record PixKeyView(PixKeyType Type, string Masked);

/// <summary>Entry point, public so the integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program
{
}
