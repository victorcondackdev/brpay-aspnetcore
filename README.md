# BrPay.AspNetCore

[![ci](https://github.com/victorcondackdev/brpay-aspnetcore/actions/workflows/ci.yml/badge.svg)](https://github.com/victorcondackdev/brpay-aspnetcore/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

ASP.NET Core integration for Brazilian payment identifiers. It gives payment APIs three things
that every Brazilian backend ends up rewriting:

- **Validation at the edge.** DataAnnotations attributes for CPF, CNPJ (including the
  alphanumeric format the Receita Federal issues from July 2026), Pix keys, CEP and BRL amounts
  in centavos, plus a minimal API filter that turns failures into RFC 9457 problem details.
- **Types instead of strings.** `Cpf`, `Cnpj` and `PixKey` are `readonly record struct` value
  objects implementing `ISpanParsable<T>` and `IFormattable`. They bind straight from route and
  query parameters, and serialize with System.Text.Json.
- **LGPD-safe by default.** `ToString()` on every value object returns the masked form used by
  the Pix DICT (`***.982.247-**`). Interpolating a CPF into a log message or an exception text
  never leaks it; reading the raw value requires an explicit `.Value`.

Targets .NET 8, depends only on the ASP.NET Core shared framework (no NuGet dependencies).

> **Relationship with BrPay.** The CPF, CNPJ and Pix key rules are a small internal port of the Go
> library [brpay](https://github.com/victorcondackdev/brpay) (`document` and `pix` packages). A full
> .NET port, the `BrPay` package, is being prepared in
> [brpay-dotnet](https://github.com/victorcondackdev/brpay-dotnet). Once it is published on NuGet,
> this package will depend on it and drop the internal copies. The public API here will not change.

## Install

```sh
dotnet add package BrPay.AspNetCore
```

## Quick start

```csharp
using System.ComponentModel.DataAnnotations;
using BrPay.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddBrPay();                     // optional: customizes the problem title and keys

var app = builder.Build();

app.MapPost("/payers", (CreatePayer payer) => Results.Ok())
   .WithBrazilianDocumentValidation();          // 400 application/problem+json on failure

app.MapGet("/keys/{key}", (PixKey key) => new { key.Type, masked = key.ToString() });

app.Run();

public sealed record CreatePayer(
    [Required, Cpf] string Document,
    [PixKey(AllowedTypes = PixKeyTypes.Email | PixKeyTypes.Evp)] string? Key,
    [BrlCents(Min = 100, Max = 10_000_000)] long AmountCents,
    [Cep] string? PostalCode);
```

An invalid request produces:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Um ou mais campos são inválidos.",
  "status": 400,
  "errors": {
    "document": ["O campo Document deve conter um CPF válido."],
    "amountCents": ["O campo AmountCents deve ser um valor em centavos entre R$ 1,00 e R$ 100.000,00."]
  }
}
```

## Validation attributes

| Attribute | Accepts | Notes |
| --- | --- | --- |
| `[Cpf]` | `string`, `Cpf` | Punctuation optional. |
| `[Cnpj(AllowAlphanumeric = true)]` | `string`, `Cnpj` | Alphanumeric CNPJ accepted by default; set `false` for legacy-only fields. |
| `[CpfOrCnpj]` | `string`, `Cpf`, `Cnpj` | For "CPF/CNPJ" payer fields. |
| `[PixKey(AllowedTypes = PixKeyTypes.All)]` | `string`, `PixKey` | Restrict with flags: `Cpf`, `Cnpj`, `Email`, `Phone`, `Evp`, `Document`. |
| `[Cep]` | `string` | `00000-000` or `00000000`; shape only. |
| `[BrlCents(Min = 1, Max = long.MaxValue)]` | integral types | Amounts in centavos. `decimal`/`double` are rejected on purpose. |

All attributes treat `null` as valid, like the built-in ones, so presence is the job of
`[Required]`. Default messages are pt-BR; override with `ErrorMessage` (`{0}` is the display name,
honoring `[Display(Name = ...)]`).

They are plain `ValidationAttribute`s, so they also work with MVC model validation, Blazor
`EditForm`, `Validator.TryValidateObject` and Options validation.

## Minimal API validation

Minimal APIs in .NET 8 do not run DataAnnotations. Two ways to opt in:

```csharp
// 1. Convention: inspects the handler once at startup and validates
//    - attributes on handler parameters: ([FromQuery, Cnpj] string cnpj)
//    - attributes on the top-level members of bound models, including positional
//      record parameters ([Cpf] string Document) and [property: Cpf]
//    - IValidatableObject
app.MapPost("/payers", Handler).WithBrazilianDocumentValidation();

// 2. Explicit filter for a single model type.
app.MapPost("/payers", Handler).AddEndpointFilter<ValidationFilter<CreatePayer>>();
```

Endpoints with nothing to validate get no filter, so the convention costs nothing at request time
for them. Error keys follow `BrPayOptions.ErrorKeyNamingPolicy` (camelCase by default) and
`[JsonPropertyName]`. Nested objects are not validated recursively.

```csharp
builder.Services.AddBrPay(o =>
{
    o.ValidationProblemTitle = "Invalid payer data.";
    o.ErrorKeyNamingPolicy = null;               // keep CLR member names
});
```

## Value objects

```csharp
var cpf = Cpf.Parse("529.982.247-25");

cpf.Value;                // "52998224725"      raw, explicit
cpf.Formatted;            // "529.982.247-25"
cpf.ToString();           // "***.982.247-**"   masked by default
$"{cpf:F}";               // "529.982.247-25"   "M" masked, "F" formatted, "V" raw

var cnpj = Cnpj.Parse("12.abc.345/01de-35");
cnpj.Value;               // "12ABC34501DE35"
cnpj.IsAlphanumeric;      // true
cnpj.ToString();          // "12.***.***/01DE-**"

var key = PixKey.Parse("+55 (21) 98765-4321");
key.Type;                 // PixKeyType.Phone
key.Value;                // "+5521987654321"    canonical DICT / BR Code form
key.ToString();           // "+5521*****4321"
```

| Type | Canonical `Value` | Masked `ToString()` |
| --- | --- | --- |
| `Cpf` | `52998224725` | `***.982.247-**` |
| `Cnpj` | `11222333000181` | `11.***.***/0001-**` |
| `PixKey` (e-mail) | `fulano@example.com` | `f***@example.com` |
| `PixKey` (phone) | `+5521987654321` | `+5521*****4321` |
| `PixKey` (EVP) | `123e4567-e12b-12d1-a456-426655440000` | `123e4567-****-****-****-********0000` |

**Binding.** Because the types expose `static bool TryParse(string?, IFormatProvider?, out T)`,
minimal APIs bind them from route and query values with no extra code, and answer `400` when
parsing fails:

```csharp
app.MapGet("/payers/{cpf}", (Cpf cpf) => ...);          // GET /payers/529.982.247-25
app.MapGet("/keys/{key}", (PixKey key) => ...);         // GET /keys/%2B5521987654321
```

**JSON.** Each type carries a `[JsonConverter]` (`CpfJsonConverter`, `CnpjJsonConverter`,
`PixKeyJsonConverter`). Reading accepts punctuated and lowercase input and throws `JsonException`
for invalid values (which minimal APIs report as `400`). Writing emits the canonical raw value,
because wire contracts need the real data; masking is for logs, not for payloads. `default(T)` is
written as `null`.

**Pix key rules.** The type is decided by shape, exactly like `brpay/pix`: contains `@` is an
e-mail (lowercased, at most 77 characters, ASCII, dotted domain); starts with `+` is a phone
(`+55`, area code without zeros, 8 digits or 9 digits starting with 9); a 36-character UUID is an
EVP; otherwise 11 characters is a CPF and 14 a CNPJ, both with valid check digits. National phone
numbers without `+55` are rejected because an 11-digit string is indistinguishable from a CPF.

## Logging and LGPD

The masked `ToString()` is the whole mechanism, and it is deliberately boring: there is no
redaction pipeline to configure and nothing to forget to register.

```csharp
logger.LogInformation("Payer {Document} registered", cpf);   // Payer ***.982.247-** registered
throw new InvalidOperationException($"Duplicate payer {cpf}"); // masked as well
```

This covers message templates, string interpolation, `string.Format` and the built-in console
JSON formatter. Structured loggers that destructure objects by reflection (for example Serilog's
`{@Payer}`) read public properties, including `Value`; log the value object itself, not a
destructured container, or configure a destructuring policy.

## Samples

[`samples/Sample.Api`](samples/Sample.Api/Program.cs) is a runnable minimal API:

```sh
dotnet run --project samples/Sample.Api

curl -i -X POST http://localhost:5080/payers -H 'Content-Type: application/json' \
  -d '{"document":"529.982.247-24","name":"Fulana","amountCents":5}'
curl -i http://localhost:5080/keys/fulana@example.com
curl -i "http://localhost:5080/companies?cnpj=12.ABC.345/01DE-35"
```

The same project is used by the end-to-end tests through `WebApplicationFactory<Program>`.

## Building

```sh
dotnet build -c Release
dotnet test -c Release --collect:"XPlat Code Coverage"
dotnet pack src/BrPay.AspNetCore -c Release -o artifacts
```

Warnings are errors, public APIs must be documented, and builds are deterministic.

## License

[MIT](LICENSE) © 2026 Victor Condack
