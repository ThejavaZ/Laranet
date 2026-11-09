namespace Laranet.Cli.Generation;

/// <summary>
/// Resolves user-defined templates that override the built-in ones.
/// </summary>
internal sealed class TemplateCatalog
{
    private static readonly Dictionary<GeneratorKind, string> FileNames = new()
    {
        [GeneratorKind.Model] = "model.tpl",
        [GeneratorKind.Service] = "service.tpl",
        [GeneratorKind.Controller] = "controller.tpl",
        [GeneratorKind.Middleware] = "middleware.tpl",
        [GeneratorKind.Repository] = "repository.tpl",
    };

    private readonly string? directory;
    private readonly Dictionary<GeneratorKind, string?> overrides = [];

    private TemplateCatalog(string? directory)
    {
        this.directory = directory;
    }

    /// <summary>
    /// Finds the nearest <c>.laranet/templates</c> directory, starting at the output directory and walking up.
    /// </summary>
    /// <param name="outputDirectory">The directory where code will be generated.</param>
    /// <returns>A catalog that may or may not contain overrides.</returns>
    public static TemplateCatalog Discover(string outputDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(outputDirectory));

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".laranet", "templates");

            if (Directory.Exists(candidate))
            {
                return new TemplateCatalog(candidate);
            }

            directory = directory.Parent;
        }

        return new TemplateCatalog(null);
    }

    /// <summary>
    /// Gets the user-defined template for a generator, if one exists.
    /// </summary>
    /// <param name="kind">The generator whose template should be resolved.</param>
    /// <returns>The template content, or <see langword="null"/> to use the built-in template.</returns>
    /// <exception cref="CliException">The template file is empty or cannot be read.</exception>
    public string? Find(GeneratorKind kind)
    {
        if (directory is null || !FileNames.TryGetValue(kind, out var fileName))
        {
            return null;
        }

        if (overrides.TryGetValue(kind, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(directory, fileName);
        string? content = null;

        if (File.Exists(path))
        {
            content = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new CliException($"The template '{path}' is empty.");
            }
        }

        overrides[kind] = content;

        return content;
    }
}
