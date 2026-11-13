namespace Laranet.Cli.Generation;

/// <summary>
/// Generates a model class.
/// </summary>
internal sealed class ModelGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var target = GeneratorSupport.ResolveTarget(context, "Models");
        var typeName = context.Name;

        return new GenerationPlan(context.Name, [
            new GeneratedFile(
                Path.Combine(target.Directory, $"{typeName}.cs"),
                GeneratorSupport.Render(
                    GeneratorKind.Model,
                    context,
                    target.Namespace,
                    typeName,
                    CodeTemplates.Model)),
        ]);
    }
}
