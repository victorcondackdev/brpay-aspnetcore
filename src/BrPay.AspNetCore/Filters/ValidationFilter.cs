using BrPay.AspNetCore.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BrPay.AspNetCore;

/// <summary>
/// Minimal API endpoint filter that runs DataAnnotations (including <see cref="CpfAttribute"/>,
/// <see cref="CnpjAttribute"/>, <see cref="PixKeyAttribute"/> and friends) on every argument of
/// type <typeparamref name="T"/> and short-circuits with an RFC 9457 validation problem
/// (<c>400</c>, <c>application/problem+json</c>) when any rule fails.
/// </summary>
/// <typeparam name="T">The bound model type to validate.</typeparam>
/// <example>
/// <code>
/// app.MapPost("/payers", (CreatePayer payer) => Results.Ok())
///    .AddEndpointFilter&lt;ValidationFilter&lt;CreatePayer&gt;&gt;();
/// </code>
/// </example>
public sealed class ValidationFilter<T> : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var options = ValidationProblems.GetOptions(context.HttpContext);
        Dictionary<string, List<string>>? errors = null;
        foreach (var argument in context.Arguments)
        {
            if (argument is T model)
            {
                errors ??= new Dictionary<string, List<string>>(StringComparer.Ordinal);
                ModelValidator.Validate(model, options, errors);
            }
        }

        if (errors is { Count: > 0 })
        {
            return ValidationProblems.Create(errors, options);
        }

        return await next(context);
    }
}

internal static class ValidationProblems
{
    private static readonly BrPayOptions DefaultOptions = new();

    public static BrPayOptions GetOptions(HttpContext httpContext) =>
        httpContext.RequestServices.GetService<IOptions<BrPayOptions>>()?.Value ?? DefaultOptions;

    public static IResult Create(Dictionary<string, List<string>> errors, BrPayOptions options) =>
        Results.ValidationProblem(ModelValidator.ToProblemErrors(errors), title: options.ValidationProblemTitle);
}
