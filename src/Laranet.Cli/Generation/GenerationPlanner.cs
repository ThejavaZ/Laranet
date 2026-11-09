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
        bool flat,
        TemplateCatalog templates)
    {
        IReadOnlyList<GeneratedFile> files = kind switch
        {
            GeneratorKind.Model =>
                [CreateModel(name, baseNamespace, outputDirectory, flat, templates)],
            GeneratorKind.Service =>
                [CreateService(name, baseNamespace, outputDirectory, flat, templates)],
            GeneratorKind.Controller =>
                [CreateController(name, baseNamespace, outputDirectory, flat, templates)],
            GeneratorKind.Middleware =>
                [CreateMiddleware(name, baseNamespace, outputDirectory, flat, templates)],
            GeneratorKind.Resource =>
            [
                CreateModel(name, baseNamespace, outputDirectory, flat, templates),
                CreateService(name, baseNamespace, outputDirectory, flat, templates),
                CreateController(name, baseNamespace, outputDirectory, flat, templates),
            ],
            GeneratorKind.Repository =>
                [CreateRepository(name, baseNamespace, outputDirectory, flat, templates)],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        return new GenerationPlan(name, files);
    }

    private static GeneratedFile CreateModel(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat,
        TemplateCatalog templates)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Models", flat);
        var typeName = name;

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            Render(
                GeneratorKind.Model,
                templates,
                target.Namespace,
                typeName,
                CodeTemplates.Model));
    }

    private static GeneratedFile CreateService(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat,
        TemplateCatalog templates)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Services", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Service");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            Render(
                GeneratorKind.Service,
                templates,
                target.Namespace,
                typeName,
                CodeTemplates.Service));
    }

    private static GeneratedFile CreateController(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat,
        TemplateCatalog templates)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Controllers", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Controller");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            Render(
                GeneratorKind.Controller,
                templates,
                target.Namespace,
                typeName,
                CodeTemplates.Controller));
    }

    private static GeneratedFile CreateMiddleware(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat,
        TemplateCatalog templates)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Middleware", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Middleware");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            Render(
                GeneratorKind.Middleware,
                templates,
                target.Namespace,
                typeName,
                CodeTemplates.Middleware));
    }

    private static GeneratedFile CreateRepository(
        string name,
        string baseNamespace,
        string outputDirectory,
        bool flat,
        TemplateCatalog templates)
    {
        var target = ResolveTarget(outputDirectory, baseNamespace, "Repositories", flat);
        var typeName = NameNormalizer.EnsureSuffix(name, "Repository");

        return new GeneratedFile(
            Path.Combine(target.Directory, $"{typeName}.cs"),
            Render(
                GeneratorKind.Repository,
                templates,
                target.Namespace,
                typeName,
                CodeTemplates.Repository));
    }

    private static string Render(
        GeneratorKind kind,
        TemplateCatalog templates,
        string @namespace,
        string name,
        Func<string, string, string> builtIn)
    {
        return templates.Find(kind) is { } template
            ? CodeTemplates.Render(template, @namespace, name)
            : builtIn(@namespace, name);
    }

    private static (string Directory, string Namespace) ResolveTarget(
        string outputDirectory,
        string baseNamespace,
        string folder,
        bool flat)
    {
        return flat
            ? (outputDirectory, baseNamespace)
            : (Path.Combine(outputDirectory, folder), $"{baseNamespace}.{folder}");
    }
}
