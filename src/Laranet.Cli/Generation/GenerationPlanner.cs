namespace Laranet.Cli.Generation;

/// <summary>
/// Builds the file plan for each generator.
/// </summary>
internal static class GenerationPlanner
{
    public static GenerationPlan Create(
        GeneratorKind kind,
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        IReadOnlyList<GeneratedFile> files = kind switch
        {
            GeneratorKind.Model => [CreateModel(name, baseNamespace, outputDirectory, flat)],
            GeneratorKind.Service => [CreateService(name, baseNamespace, outputDirectory, flat)],
            GeneratorKind.Controller => [CreateController(name, baseNamespace, outputDirectory, flat)],
            GeneratorKind.Middleware => [CreateMiddleware(name, baseNamespace, outputDirectory, flat)],
            GeneratorKind.Resource =>
            [
                CreateModel(name, baseNamespace, outputDirectory, flat),
                CreateService(name, baseNamespace, outputDirectory, flat),
                CreateController(name, baseNamespace, outputDirectory, flat),
            ],
            GeneratorKind.Repository => [CreateRepository(name, baseNamespace, outputDirectory, flat)],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        return new GenerationPlan(name, files);
    }

    private static GeneratedFile CreateModel(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Models", flat);
        var typeName = name;

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            CodeTemplates.Model(target.Namespace, typeName));
    }

    private static GeneratedFile CreateService(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Services", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Service");

        return new GeneratedFile(
            System.IO.Path.Combine(target.Directory, $"{typeName}.cs"),
            CodeTemplates.Service(target.Namespace, typeName));
    }

    private static GeneratedFile CreateController(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Controllers", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Controller");

        return new GeneratedFile(
            System.IO.Path.Combine(target.Directory, $"{typeName}.cs"),
            CodeTemplates.Controller(target.Namespace, typeName));
    }

    private static GeneratedFile CreateMiddleware(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Middleware", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Middleware");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            CodeTemplates.Middleware(target.Namespace, typeName));
    }

    private static GeneratedFile CreateRepository(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Repositories", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Repository");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            CodeTemplates.Repository(target.Namespace, typeName));
    }

    private static (string Directory, string Namespace) ResolveTarget(
        string outputDirectory,
        string baseNamespace,
        string folder,
        bool flat)
    {
        return flat
            ? (outputDirectory, baseNamespace)
            : (System.IO.Path.Combine(outputDirectory, folder), $"{baseNamespace}.{folder}");
    }
}
