namespace Laranet.Cli.Generation;

/// <summary>
/// Generates an optional repository scaffold.
/// </summary>
internal sealed class RepositoryGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var target = GeneratorSupport.ResolveTarget(context, "Repositories");
        var typeName = NameNormalizer.EnsureSuffix(context.Name, "Repository");

        return new GenerationPlan(context.Name, [
            new GeneratedFile(
                Path.Combine(target.Directory, $"{typeName}.cs"),
                GeneratorSupport.Render(
                    GeneratorKind.Repository,
                    context,
                    target.Namespace,
                    typeName,
                    CodeTemplates.Repository)),
        ]);
    }
}
