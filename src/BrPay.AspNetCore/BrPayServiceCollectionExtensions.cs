using BrPay.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the BrPay ASP.NET Core integration.</summary>
public static class BrPayServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BrPayOptions"/>. Value objects, attributes and JSON converters work
    /// without registration; this only customizes the validation problem details.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of <see cref="BrPayOptions"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddBrPay(this IServiceCollection services, Action<BrPayOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BrPayOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
