namespace Laranet.Cli.Generation;

/// <summary>
/// Produces the file plan for a generator independently of the command-line layer.
/// </summary>
internal interface IGenerator
{
    /// <summary>
    /// Creates the files for the supplied context without writing to disk.
    /// </summary>
    /// <param name="context">The generation inputs.</param>
    /// <returns>The planned files.</returns>
    GenerationPlan Create(GenerationContext context);
}
