namespace Laranet.Cli.Generation;

/// <summary>
/// Generates an ASP.NET Core middleware.
/// </summary>
internal sealed class MiddlewareGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var target = GeneratorSupport.ResolveTarget(context, "Middleware");
        var typeName = NameNormalizer.EnsureSuffix(context.Name, "Middleware");

        return new GenerationPlan(context.Name, [
            new GeneratedFile(
                Path.Combine(target.Directory, $"{typeName}.cs"),
                GeneratorSupport.Render(
                    GeneratorKind.Middleware,
                    context,
                    target.Namespace,
                    typeName,
                    CodeTemplates.Middleware)),
        ]);
    }
}
