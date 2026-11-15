namespace Laranet.Cli.Generation;

/// <summary>
/// Generates native options and dependency-injection registration code.
/// </summary>
internal sealed class ConfigurationGenerator : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var options = NameNormalizer.EnsureSuffix(context.Name, "Options");
        var baseName = options[..^"Options".Length];
        var sections = context.Settings?.Sections is { Count: > 0 } configured
            ? configured
            : [baseName];
        var target = GeneratorSupport.ResolveTarget(context, "Configuration");
        var files = new[]
        {
            new GeneratedFile(
                Path.Combine(target.Directory, $"{options}.cs"),
                ConfigurationTemplates.Options(target.Namespace, options, sections)),
            new GeneratedFile(
                Path.Combine(target.Directory, $"{baseName}ServiceCollectionExtensions.cs"),
                ConfigurationTemplates.Registration(target.Namespace, baseName, options, sections)),
        };

        return new GenerationPlan(context.Name, files);
    }
}
