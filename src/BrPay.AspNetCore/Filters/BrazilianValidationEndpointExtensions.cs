using System.ComponentModel.DataAnnotations;
using System.Reflection;
using BrPay.AspNetCore;
using BrPay.AspNetCore.Internal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Endpoint conventions for validating Brazilian payment data in minimal APIs.</summary>
public static class BrazilianValidationEndpointExtensions
{
    /// <summary>
    /// Validates the endpoint's arguments with DataAnnotations before the handler runs and returns
    /// an RFC 9457 validation problem (<c>400</c>) on failure. Covers attributes on handler
    /// parameters (<c>([FromQuery, Cpf] string document)</c>) and on the top-level members of bound
    /// models, including positional record parameters (<c>record Payer([Cpf] string Document)</c>).
    /// </summary>
    /// <remarks>
    /// The handler signature is inspected once, when the endpoint is built; endpoints without any
    /// validation attribute get no filter at all. Nested objects are not validated recursively.
    /// </remarks>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static RouteHandlerBuilder WithBrazilianDocumentValidation(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory(static (factoryContext, next) =>
        {
            var plan = ValidationPlan.Create(factoryContext.MethodInfo);
            if (plan.Length == 0)
            {
                return next;
            }

            return async invocationContext =>
            {
                var options = ValidationProblems.GetOptions(invocationContext.HttpContext);
                var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                foreach (var step in plan)
                {
                    if (step.Index >= invocationContext.Arguments.Count)
                    {
                        continue;
                    }

                    var argument = invocationContext.Arguments[step.Index];
                    if (step.ParameterAttributes.Length > 0)
                    {
                        ModelValidator.ValidateValue(argument, invocationContext.HttpContext, step.Name, step.ParameterAttributes, errors);
                    }

                    if (step.ValidateMembers && argument is not null)
                    {
                        ModelValidator.Validate(argument, options, errors);
                    }
                }

                if (errors.Count > 0)
                {
                    return ValidationProblems.Create(errors, options);
                }

                return await next(invocationContext);
            };
        });

        return builder;
    }

    internal sealed record ValidationStep(int Index, string Name, ValidationAttribute[] ParameterAttributes, bool ValidateMembers);

    internal static class ValidationPlan
    {
        public static ValidationStep[] Create(MethodInfo method)
        {
            var steps = new List<ValidationStep>();
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var attributes = parameter.GetCustomAttributes<ValidationAttribute>().ToArray();
                var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                var validateMembers = IsCandidateModel(type) && ModelValidator.IsValidatable(type);
                if (attributes.Length > 0 || validateMembers)
                {
                    steps.Add(new ValidationStep(i, parameter.Name ?? "arg" + i, attributes, validateMembers));
                }
            }

            return steps.ToArray();
        }

        private static bool IsCandidateModel(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                return false;
            }

            // Framework services and primitives (HttpContext, CancellationToken, Guid, ...) never
            // carry user validation attributes; skipping them keeps startup reflection cheap.
            var ns = type.Namespace;
            return ns is null || !(ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns.StartsWith("Microsoft.", StringComparison.Ordinal));
        }
    }
}
