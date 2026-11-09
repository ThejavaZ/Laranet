using System.Text;

namespace Laranet.Cli.Generation;

/// <summary>
/// Writes a generation plan to disk or prints its intended actions.
/// </summary>
internal sealed class GenerationWriter(
    TextWriter output,
    TextWriter error,
    string displayBaseDirectory)
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false);

    public int Write(GenerationPlan plan, bool force, bool dryRun)
    {
        var conflicts = plan.Files
            .Where(file => File.Exists(file.Path))
            .ToArray();

        if (conflicts.Length > 0 && !force)
        {
            foreach (var conflict in conflicts)
            {
                error.WriteLine(
                    $"error: {DisplayPath(conflict.Path)} already exists. Use --force to overwrite.");
            }

            return 1;
        }

        foreach (var file in plan.Files)
        {
            var exists = File.Exists(file.Path);

            if (dryRun)
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
