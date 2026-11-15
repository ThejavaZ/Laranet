namespace Laranet.Cli.Generation;

/// <summary>
/// Generates native EF Core context, configuration, and design-time factory code.
/// </summary>
internal sealed class DatabaseGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var settings = context.Settings ?? new GenerationSettings();
        var entities = settings.Entities ?? [];
        var contextTarget = ResolveTarget(context, "DbContexts");
        var configurationTarget = ResolveTarget(context, "Configurations");
        var factoryTarget = ResolveTarget(context, "Factories");
        var contextName = NameNormalizer.EnsureSuffix(context.Name, "Context");
        var files = new List<GeneratedFile>
        {
            new(
                Path.Combine(contextTarget.Directory, $"{contextName}.cs"),
                DatabaseTemplates.Context(
                    context,
                    contextTarget.Namespace,
                    contextName,
                    entities)),
            new(
                Path.Combine(factoryTarget.Directory, $"{contextName}Factory.cs"),
                DatabaseTemplates.Factory(
                    context,
                    factoryTarget.Namespace,
                    contextTarget.Namespace,
                    contextName,
                    settings.Provider)),
        };

        foreach (var entity in entities)
        {
            files.Add(
                new GeneratedFile(
                    Path.Combine(configurationTarget.Directory, $"{entity}Configuration.cs"),
                    DatabaseTemplates.Configuration(
                        context,
                        configurationTarget.Namespace,
                        entity)));
        }

        return new GenerationPlan(context.Name, files);
    }

    private static (string Directory, string Namespace) ResolveTarget(
        GenerationContext context,
        string folder)
    {
        return GeneratorSupport.ResolveTarget(context, folder);
    }
}
