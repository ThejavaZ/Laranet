namespace Laranet.Cli.Generation;

/// <summary>
/// Represents the files produced by a generator.
/// </summary>
/// <param name="Name">The normalized generator name.</param>
/// <param name="Files">The files to create.</param>
internal sealed record GenerationPlan(string Name, IReadOnlyList<GeneratedFile> Files);
