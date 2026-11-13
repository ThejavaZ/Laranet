namespace Laranet.Cli.Generation;

/// <summary>
/// Generates a service class.
/// </summary>
internal sealed class ServiceGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var target = GeneratorSupport.ResolveTarget(context, "Services");
        var typeName = NameNormalizer.EnsureSuffix(context.Name, "Service");

        return new GenerationPlan(context.Name, [
            new GeneratedFile(
                Path.Combine(target.Directory, $"{typeName}.cs"),
                GeneratorSupport.Render(
                    GeneratorKind.Service,
                    context,
                    target.Namespace,
                    typeName,
                    CodeTemplates.Service)),
        ]);
    }
}
