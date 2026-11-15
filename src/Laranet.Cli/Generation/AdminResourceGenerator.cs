namespace Laranet.Cli.Generation;

/// <summary>
/// Generates an Admin resource class.
/// </summary>
internal sealed class AdminResourceGenerator(string? policy) : IGenerator
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
        var policyName = string.IsNullOrWhiteSpace(policy)
            ? $"Admin.{entityName}"
            : policy!;
        var target = GeneratorSupport.ResolveTarget(context, "Admin", "Resources");
        var content = AdminResourceTemplates.Render(
            target.Namespace,
            resourceName,
            entityName,
            fields,
            policyName);

        return new GenerationPlan(
            context.Name,
            [new GeneratedFile(Path.Combine(target.Directory, $"{resourceName}.cs"), content)]);
    }
}
