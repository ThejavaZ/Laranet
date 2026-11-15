namespace Laranet.Cli.Generation;

internal static class ConfigurationTemplates
{
    public static string Options(
        string @namespace,
        string options,
        IReadOnlyList<string> sections)
    {
        var constants = string.Join(
            Environment.NewLine,
            sections.Select((section, index) =>
            {
                var name = index == 0
                    ? "SectionName"
                    : $"{ToIdentifier(section)}SectionName";
                return $"    public const string {name} = \"{section}\";";
            }));

        return $$"""
            namespace {{@namespace}};

            public sealed class {{options}}
            {
            {{constants}}

                public string? ConnectionString { get; init; }

                public bool Enabled { get; init; }

                public int TimeoutSeconds { get; init; }
            }

            """;
    }

    public static string Registration(
        string @namespace,
        string baseName,
        string options,
        IReadOnlyList<string> sections)
    {
        var registrations = string.Join(
            Environment.NewLine,
            sections.Select((section, index) =>
            {
                var name = index == 0
                    ? $"{options}.SectionName"
                    : $"{options}.{ToIdentifier(section)}SectionName";
                var registration = index == 0
                    ? $"services.AddOptions<{options}>()"
                    : $"services.AddOptions<{options}>(\"{section}\")";

                return $$"""
                        {{registration}}
                            .Bind(configuration.GetSection({{name}}))
                            .ValidateDataAnnotations()
                            .ValidateOnStart();
                """;
            }));

        return $$"""
            using Microsoft.Extensions.Configuration;
            using Microsoft.Extensions.DependencyInjection;

            namespace {{@namespace}};

            public static class {{baseName}}ServiceCollectionExtensions
            {
                public static IServiceCollection Add{{baseName}}Options(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
            {{registrations}}

                    return services;
                }
            }

            """;
    }

    private static string ToIdentifier(string value)
    {
        var normalized = NameNormalizer.Normalize(value.Replace(':', '-'));

        return normalized;
    }
}
