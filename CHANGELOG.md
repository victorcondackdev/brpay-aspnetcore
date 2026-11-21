# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned

- Depend on the `BrPay` NuGet package for CPF, CNPJ and Pix key rules once it is published,
  replacing the internal copies in `src/BrPay.AspNetCore/Internal`.

## [0.1.0] - 2026-11-21

### Added

- DataAnnotations attributes with pt-BR default messages: `[Cpf]`, `[Cnpj]` (alphanumeric CNPJ
  accepted by default), `[CpfOrCnpj]`, `[PixKey(AllowedTypes = ...)]`, `[Cep]` and
  `[BrlCents(Min, Max)]`.
- `WithBrazilianDocumentValidation()` minimal API convention and `ValidationFilter<T>` endpoint
  filter returning RFC 9457 validation problem details.
- `Cpf`, `Cnpj` and `PixKey` value objects implementing `ISpanParsable<T>` and `IFormattable`,
  bindable from route and query parameters, with System.Text.Json converters.
- Masked `ToString()` on every value object for LGPD-friendly logging.
- `AddBrPay()` service registration with `BrPayOptions`.

[Unreleased]: https://github.com/victorcondackdev/brpay-aspnetcore/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/victorcondackdev/brpay-aspnetcore/releases/tag/v0.1.0
