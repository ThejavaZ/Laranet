namespace Laranet.Cli.Generation;

/// <summary>
/// Generates a single API resource view model.
/// </summary>
internal sealed class ApiResourceGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var settings = context.Settings ?? new GenerationSettings();
        var fields = settings.Fields ?? [];
        var entityName = context.Name.EndsWith("Resource", StringComparison.Ordinal)
            ? context.Name[..^"Resource".Length]
            : context.Name;
        if (string.IsNullOrWhiteSpace(entityName))
        {
            entityName = context.Name;
        }

        var resourceName = entityName + "Resource";
        var target = GeneratorSupport.ResolveTarget(context, "Contracts");
        var content = ApiResourceTemplates.Render(
            target.Namespace,
            resourceName,
            entityName,
            fields);

        return new GenerationPlan(
            context.Name,
            [new GeneratedFile(Path.Combine(target.Directory, $"{resourceName}.cs"), content)]);
    }
}
