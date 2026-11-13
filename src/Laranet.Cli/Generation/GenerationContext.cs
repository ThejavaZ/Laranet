namespace Laranet.Cli.Generation;

/// <summary>
/// Inputs shared by all code generators.
/// </summary>
/// <param name="Name">The normalized type name.</param>
/// <param name="BaseNamespace">The base namespace of the target project.</param>
/// <param name="OutputDirectory">The absolute output directory.</param>
/// <param name="Flat">Whether files go directly into the output directory.</param>
/// <param name="Templates">The templates that may override the built-in content.</param>
internal sealed record GenerationContext(
    string Name,
    string BaseNamespace,
    string OutputDirectory,
    bool Flat,
    TemplateCatalog Templates);
