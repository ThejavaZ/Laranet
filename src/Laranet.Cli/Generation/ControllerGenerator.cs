namespace Laranet.Cli.Generation;

/// <summary>
/// Generates an ASP.NET Core controller.
/// </summary>
internal sealed class ControllerGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var target = GeneratorSupport.ResolveTarget(context, "Controllers");
        var typeName = NameNormalizer.EnsureSuffix(context.Name, "Controller");

        return new GenerationPlan(context.Name, [
            new GeneratedFile(
                Path.Combine(target.Directory, $"{typeName}.cs"),
                GeneratorSupport.Render(
                    GeneratorKind.Controller,
                    context,
                    target.Namespace,
                    typeName,
                    CodeTemplates.Controller)),
        ]);
    }
}
