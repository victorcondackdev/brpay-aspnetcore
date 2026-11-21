using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace BrPay.AspNetCore.Internal;

/// <summary>
/// Runs DataAnnotations on the top-level members of a bound model. Attributes are read from
/// properties and, for positional records, from the matching primary constructor parameters,
/// so both <c>[Cpf] string Document</c> and <c>[property: Cpf] string Document</c> work.
/// </summary>
internal static class ModelValidator
{
    private static readonly ConcurrentDictionary<Type, MemberValidator[]> Cache = new();

    internal sealed record MemberValidator(PropertyInfo Property, string? JsonName, string DisplayName, ValidationAttribute[] Attributes);

    public static bool IsValidatable(Type type) =>
        typeof(IValidatableObject).IsAssignableFrom(type) || GetValidators(type).Length > 0;

    public static MemberValidator[] GetValidators(Type type) => Cache.GetOrAdd(type, Build);

    public static void Validate(object instance, BrPayOptions options, Dictionary<string, List<string>> errors)
    {
        foreach (var member in GetValidators(instance.GetType()))
        {
            var value = member.Property.GetValue(instance);
            var context = new ValidationContext(instance)
            {
                MemberName = member.Property.Name,
                DisplayName = member.DisplayName,
            };

            var results = new List<ValidationResult>();
            if (!Validator.TryValidateValue(value, context, results, member.Attributes))
            {
                var key = member.JsonName ?? ConvertName(member.Property.Name, options);
                foreach (var result in results)
                {
                    Add(errors, key, result.ErrorMessage);
                }
            }
        }

        if (instance is IValidatableObject validatable)
        {
            foreach (var result in validatable.Validate(new ValidationContext(instance)))
            {
                var added = false;
                foreach (var memberName in result.MemberNames)
                {
                    Add(errors, ConvertName(memberName, options), result.ErrorMessage);
                    added = true;
                }

                if (!added)
                {
                    Add(errors, string.Empty, result.ErrorMessage);
                }
            }
        }
    }

    public static void ValidateValue(
        object? value,
        object contextInstance,
        string name,
        IReadOnlyList<ValidationAttribute> attributes,
        Dictionary<string, List<string>> errors)
    {
        var context = new ValidationContext(contextInstance) { MemberName = name, DisplayName = name };
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateValue(value, context, results, attributes))
        {
            foreach (var result in results)
            {
                Add(errors, name, result.ErrorMessage);
            }
        }
    }

    public static Dictionary<string, string[]> ToProblemErrors(Dictionary<string, List<string>> errors)
    {
        var problem = new Dictionary<string, string[]>(errors.Count, StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            problem[pair.Key] = pair.Value.ToArray();
        }

        return problem;
    }

    private static string ConvertName(string name, BrPayOptions options) =>
        options.ErrorKeyNamingPolicy is { } policy && name.Length > 0 ? policy.ConvertName(name) : name;

    private static void Add(Dictionary<string, List<string>> errors, string key, string? message)
    {
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }

        list.Add(message ?? "The value is invalid.");
    }

    private static MemberValidator[] Build(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type.IsPointer || type == typeof(string) || type == typeof(decimal))
        {
            return [];
        }

        var parametersByName = new Dictionary<string, List<ParameterInfo>>(StringComparer.Ordinal);
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (parameter.Name is null)
                {
                    continue;
                }

                if (!parametersByName.TryGetValue(parameter.Name, out var list))
                {
                    list = new List<ParameterInfo>();
                    parametersByName[parameter.Name] = list;
                }

                list.Add(parameter);
            }
        }

        var validators = new List<MemberValidator>();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var attributes = new List<ValidationAttribute>(property.GetCustomAttributes<ValidationAttribute>(inherit: true));
            if (parametersByName.TryGetValue(property.Name, out var parameters))
            {
                foreach (var parameter in parameters)
                {
                    foreach (var attribute in parameter.GetCustomAttributes<ValidationAttribute>())
                    {
                        if (!attributes.Exists(a => a.GetType() == attribute.GetType()))
                        {
                            attributes.Add(attribute);
                        }
                    }
                }
            }

            if (attributes.Count == 0)
            {
                continue;
            }

            // RequiredAttribute first, as MVC does, so "required" is reported before format errors.
            attributes.Sort(static (a, b) => (b is RequiredAttribute).CompareTo(a is RequiredAttribute));

            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            var displayName = property.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? property.Name;
            validators.Add(new MemberValidator(property, jsonName, displayName, attributes.ToArray()));
        }

        return validators.ToArray();
    }
}
