namespace Laranet.Cli.Generation;

/// <summary>
/// Shared helpers for convention-based code generators.
/// </summary>
internal static class GeneratorSupport
{
    /// <summary>
    /// Resolves the target directory and namespace for convention folders.
    /// </summary>
    /// <param name="context">The generation inputs.</param>
    /// <param name="folders">The convention folders, such as <c>Models</c>.</param>
    /// <returns>The directory and namespace for the generated file.</returns>
    public static (string Directory, string Namespace) ResolveTarget(
        GenerationContext context,
        params string[] folders)
    {
        return context.Flat || folders.Length == 0
            ? (context.OutputDirectory, context.BaseNamespace)
            : (
                Path.Combine([context.OutputDirectory, .. folders]),
                $"{context.BaseNamespace}.{string.Join('.', folders)}");
    }

    /// <summary>
    /// Renders the user override for a generator, or the built-in template.
    /// </summary>
    /// <param name="kind">The generator whose template should be resolved.</param>
    /// <param name="context">The generation inputs.</param>
    /// <param name="namespace">The namespace for the generated file.</param>
    /// <param name="typeName">The normalized type name.</param>
    /// <param name="builtIn">The built-in template renderer.</param>
    /// <returns>The rendered content.</returns>
    public static string Render(
        GeneratorKind kind,
        GenerationContext context,
        string @namespace,
        string typeName,
        Func<string, string, string> builtIn)
    {
        return context.Templates.Find(kind) is { } template
            ? CodeTemplates.Render(template, @namespace, typeName)
            : builtIn(@namespace, typeName);
    }
}
