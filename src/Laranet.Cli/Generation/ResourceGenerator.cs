namespace Laranet.Cli.Generation;

/// <summary>
/// Generates a model, a service, and a controller as one plan.
/// </summary>
internal sealed class ResourceGenerator : IGenerator
{
    private readonly ModelGenerator model = new();
    private readonly ServiceGenerator service = new();
    private readonly ControllerGenerator controller = new();

    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var files = model.Create(context).Files
            .Concat(service.Create(context).Files)
            .Concat(controller.Create(context).Files)
            .ToArray();

        return new GenerationPlan(context.Name, files);
    }
}
