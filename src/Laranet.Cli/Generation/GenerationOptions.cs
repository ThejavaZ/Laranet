namespace Laranet.Cli.Generation;

/// <summary>
/// Controls how a generation plan is written.
/// </summary>
/// <param name="Force">Whether existing files may be overwritten.</param>
/// <param name="DryRun">Whether to report actions without writing files.</param>
internal sealed record GenerationOptions(bool Force = false, bool DryRun = false);
