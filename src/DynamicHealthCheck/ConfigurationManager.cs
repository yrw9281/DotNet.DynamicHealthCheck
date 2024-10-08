using DynamicHealthCheck.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DynamicHealthCheck;

internal static class ConfigurationManager
{
    // Dictionary to hold the mapping between IHealthCheck and its config type
    private static readonly Dictionary<Type, Type> ConfigsDictionary = new();

    // Default config section
    public static string ConfigSection { get; set; } = Constants.DEFAULT_CONFIG_SECTION_NAME;

    // Method to register a health check with its configuration type
    public static void SetHealthCheckContext<THealthCheck, TContext>()
        where THealthCheck : class, IHealthCheck
        where TContext : class
    {
        if (!ConfigsDictionary.ContainsKey(typeof(THealthCheck)))
            ConfigsDictionary[typeof(THealthCheck)] = typeof(TContext);
    }

    // Method to get the config context for a health check
    public static TContext GetHealthCheckContext<THealthCheck, TContext>(IConfiguration configuration,
        string serviceName)
        where THealthCheck : class, IHealthCheck
        where TContext : class
    {
        if (!ConfigsDictionary.TryGetValue(typeof(THealthCheck), out var configType) ||
            configType != typeof(TContext))
            throw new ArgumentException($"The type {typeof(THealthCheck).Name} has not been well configured.");

        // Get context object for service. If ServiceName is null, get the first context as default.
        var context = GetHealthCheckConfigSection(configuration)
            .GetChildren()
            .FirstOrDefault(s =>
                s.GetChildren().Any(c =>
                    string.IsNullOrEmpty(serviceName) ||
                    (c.Key == Constants.PROPERTY_NAME_SERVICENAME && c.Value == serviceName)))?
            .GetSection(Constants.PROPERTY_NAME_CONTEXT)
            .Get<TContext>();

        return context
               ?? throw new ArgumentException($"The type {typeof(THealthCheck).Name} has not been well configured.");
    }

    // Method to get the config context list for health check
    public static IEnumerable<TContext> GetHealthCheckContexts<THealthCheck, TContext>(IConfiguration configuration)
        where THealthCheck : class, IHealthCheck
        where TContext : class
    {
        if (!ConfigsDictionary.TryGetValue(typeof(THealthCheck), out var configType) ||
            configType != typeof(TContext))
            throw new ArgumentException($"The type {typeof(THealthCheck).Name} has not been well configured.");

        var sections = GetHealthCheckConfigSection(configuration).GetChildren();

        var result = sections.Select(section => 
            section.GetSection(Constants.PROPERTY_NAME_CONTEXT)?.Get<TContext>())
            .OfType<TContext>()
            .ToList();

        return result
               ?? throw new ArgumentException($"The type {typeof(THealthCheck).Name} has not been well configured.");
    }

    // Get root configuration
    public static DynamicHealthCheckRoot? Get(IConfiguration configuration)
    {
        return GetRootSection(configuration).Get<DynamicHealthCheckRoot>();
    }

    // Get health check config
    public static DynamicHealthCheckConfig GetHealthCheckConfig<THealthCheck>(IConfiguration configuration,
        string serviceName)
        where THealthCheck : class, IHealthCheck
    {
        return GetHealthCheckConfigsByHealthCheckName(configuration, typeof(THealthCheck).Name)?
                   .FirstOrDefault(s => s.ServiceName == serviceName)
               ?? throw new ArgumentException($"The type {typeof(THealthCheck).Name} has not been well configured.");
    }

    private static IConfigurationSection GetRootSection(IConfiguration configuration)
    {
        return configuration.GetSection(ConfigSection);
    }

    private static IConfigurationSection GetHealthCheckConfigSection(IConfiguration configuration)
    {
        return GetRootSection(configuration).GetSection(Constants.PROPERTY_NAME_HEALTHCHECHS);
    }

    private static IEnumerable<DynamicHealthCheckConfig>? GetHealthCheckConfigsByHealthCheckName(
        IConfiguration configuration,
        string healthCheckName)
    {
        return GetHealthCheckConfigSection(configuration).Get<List<DynamicHealthCheckConfig>>()?
            .Where(c => c.HealthCheckName == healthCheckName);
    }
}