namespace Laranet.Cli.Generation;

/// <summary>
/// Resolves the generator registered for a <see cref="GeneratorKind"/>.
/// </summary>
internal static class GeneratorRegistry
{
    private static readonly IReadOnlyDictionary<GeneratorKind, IGenerator> Generators =
        new Dictionary<GeneratorKind, IGenerator>
        {
            [GeneratorKind.Model] = new ModelGenerator(),
            [GeneratorKind.Service] = new ServiceGenerator(),
            [GeneratorKind.Controller] = new ControllerGenerator(),
            [GeneratorKind.Middleware] = new MiddlewareGenerator(),
            [GeneratorKind.Repository] = new RepositoryGenerator(),
            [GeneratorKind.Resource] = new ResourceGenerator(),
        };

    /// <summary>
    /// Gets the generator registered for a kind.
    /// </summary>
    /// <param name="kind">The generator kind.</param>
    /// <returns>The registered generator.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The kind has no registered generator.</exception>
    public static IGenerator Resolve(GeneratorKind kind)
    {
        return Generators.TryGetValue(kind, out var generator)
            ? generator
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    /// <summary>
    /// Creates the plan for a generator kind without writing to disk.
    /// </summary>
    /// <param name="kind">The generator kind.</param>
    /// <param name="context">The generation inputs.</param>
    /// <returns>The planned files.</returns>
    public static GenerationPlan Create(GeneratorKind kind, GenerationContext context)
    {
        return Resolve(kind).Create(context);
    }
}
