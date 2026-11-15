using System.Text;
using Laranet.Cli;

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

        if (options.DryRun)
        {
            var entries = plan.Files
                .Select(file => (Path: file.Path, Relative: DisplayPath(file.Path), Exists: File.Exists(file.Path)))
                .OrderBy(entry => entry.Relative, StringComparer.Ordinal)
                .ToArray();

            output.WriteLine("Dry run:");

            RenderTree(output, entries);

            var createCount = entries.Count(entry => !entry.Exists);
            var overwriteCount = entries.Count(entry => entry.Exists);

            if (createCount > 0)
            {
                output.WriteLine($"{createCount} file(s) would be created.");
            }

            if (overwriteCount > 0)
            {
                output.WriteLine($"{overwriteCount} file(s) would be overwritten.");
            }

            return 0;
        }

        foreach (var file in plan.Files)
        {
            var exists = File.Exists(file.Path);

            var directory = Path.GetDirectoryName(file.Path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(file.Path, file.Content, Utf8WithoutBom);

            output.WriteLine(
                $"{(exists ? "overwrote" : "created")}      {DisplayPath(file.Path)}");
        }

        if (!options.DryRun && plan.Files.Count > 0)
        {
            output.WriteLine();
            CliStyle.WriteNextStep(output, "run `dotnet build` to verify the generated code.");
        }

        return 0;
    }

    private static void RenderTree(
        TextWriter output,
        IReadOnlyList<(string Path, string Relative, bool Exists)> entries)
    {
        var root = new TreeNode(string.Empty);

        foreach (var entry in entries)
        {
            var segments = entry.Relative.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var isLastSegment = i == segments.Length - 1;
                var existing = current.Children.FirstOrDefault(child => child.Name == segments[i]);

                if (existing is null)
                {
                    existing = new TreeNode(segments[i])
                    {
                        IsFile = isLastSegment,
                        Exists = isLastSegment ? entry.Exists : false,
                    };
                    current.Children.Add(existing);
                }

                current = existing;
            }
        }

        SortTree(root);
        RenderNode(output, root, string.Empty, true);
    }

    private static void SortTree(TreeNode node)
    {
        node.Children.Sort((left, right) =>
        {
            var directoryComparison = left.IsFile.CompareTo(right.IsFile);

            if (directoryComparison != 0)
            {
                return directoryComparison;
            }

            return StringComparer.Ordinal.Compare(left.Name, right.Name);
        });

        foreach (var child in node.Children)
        {
            SortTree(child);
        }
    }

    private static void RenderNode(
        TextWriter output,
        TreeNode node,
        string prefix,
        bool isLast)
    {
        if (node.Name.Length > 0)
        {
            var connector = isLast ? "└── " : "├── ";
            var status = node.IsFile
                ? (node.Exists ? "would overwrite " : "would create ") : string.Empty;
            output.WriteLine($"{prefix}{connector}{status}{node.Name}");
        }

        var childPrefix = node.Name.Length == 0
            ? string.Empty
            : prefix + (isLast ? "    " : "│   ");

        for (var i = 0; i < node.Children.Count; i++)
        {
            RenderNode(output, node.Children[i], childPrefix, i == node.Children.Count - 1);
        }
    }

    private sealed class TreeNode(string name)
    {
        public string Name { get; } = name;

        public bool IsFile { get; set; }

        public bool Exists { get; set; }

        public List<TreeNode> Children { get; } = [];
    }

    private string DisplayPath(string path)
    {
        var relative = Path.GetRelativePath(displayBaseDirectory, path);

        return relative.StartsWith("..", StringComparison.Ordinal) ? path : relative;
    }
}
