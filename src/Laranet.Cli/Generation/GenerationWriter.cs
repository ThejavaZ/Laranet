using System.Text;

namespace Laranet.Cli.Generation;

/// <summary>
/// Writes a generation plan to disk or prints its intended actions.
/// </summary>
internal sealed class GenerationWriter(
    TextWriter output,
    TextWriter error,
    string displayBaseDirectory,
    ConsoleInteraction? interaction = null)
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Writes a generation plan to disk, or reports its intended actions.
    /// </summary>
    /// <param name="plan">The files to write.</param>
    /// <param name="options">The overwrite and dry-run behavior.</param>
    /// <returns><c>0</c> when the plan was written or previewed; otherwise, an error code.</returns>
    public int Write(GenerationPlan plan, GenerationOptions options)
    {
        var force = options.Force;

        var conflicts = plan.Files
            .Where(file => File.Exists(file.Path))
            .ToArray();

        if (conflicts.Length > 0 && !force)
        {
            if (interaction is null)
            {
                foreach (var conflict in conflicts)
                {
                    error.WriteLine(
                        $"error: {DisplayPath(conflict.Path)} already exists. Use --force to overwrite.");
                }

                return 1;
            }

            output.WriteLine("The following files already exist:");

            foreach (var conflict in conflicts)
            {
                output.WriteLine($"  {DisplayPath(conflict.Path)}");
            }

            if (!interaction.Confirm("Overwrite them? [y/N] "))
            {
                error.WriteLine("error: cancelled.");

                return 1;
            }

            force = true;
        }

        foreach (var file in plan.Files)
        {
            var exists = File.Exists(file.Path);

            if (options.DryRun)
            {
                output.WriteLine(
                    $"{(exists ? "would overwrite" : "would create")} {DisplayPath(file.Path)}");

                continue;
            }

            var directory = Path.GetDirectoryName(file.Path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(file.Path, file.Content, Utf8WithoutBom);

            output.WriteLine(
                $"{(exists ? "overwrote" : "created")}      {DisplayPath(file.Path)}");
        }

        return 0;
    }

    private string DisplayPath(string path)
    {
        var relative = Path.GetRelativePath(displayBaseDirectory, path);

        return relative.StartsWith("..", StringComparison.Ordinal) ? path : relative;
    }
}
