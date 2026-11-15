namespace Laranet.Cli.Generation;

/// <summary>
/// Generates commands, queries, and handlers without adding a runtime abstraction.
/// </summary>
internal sealed class CqrsGenerator(GeneratorKind kind) : IGenerator
{
    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var settings = context.Settings ?? new GenerationSettings();
        var names = CqrsNames.Create(kind, context.Name);
        var files = new List<GeneratedFile>();

        if (kind is GeneratorKind.Command or GeneratorKind.Query)
        {
            AddRequest(files, context, settings, names);

            if (settings.WithHandler)
            {
                AddHandler(files, context, settings, names);
            }
        }
        else
        {
            AddHandler(files, context, settings, names);
        }

        if (settings.WithTests)
        {
            AddTest(files, context, settings, names);
        }

        return new GenerationPlan(context.Name, files);
    }

    private static void AddRequest(
        List<GeneratedFile> files,
        GenerationContext context,
        GenerationSettings settings,
        CqrsNames names)
    {
        var folder = names.RequestKind == GeneratorKind.Command ? "Commands" : "Queries";
        var target = ResolveTarget(context, settings.Profile, folder);
        var content = CqrsTemplates.Request(context, target.Namespace, names, settings);

        Add(files, target, $"{names.RequestType}.cs", content);
    }

    private static void AddHandler(
        List<GeneratedFile> files,
        GenerationContext context,
        GenerationSettings settings,
        CqrsNames names)
    {
        var target = ResolveTarget(context, settings.Profile, "Handlers");
        var content = CqrsTemplates.Handler(context, target.Namespace, names, settings);

        Add(files, target, $"{names.HandlerType}.cs", content);
    }

    private static void AddTest(
        List<GeneratedFile> files,
        GenerationContext context,
        GenerationSettings settings,
        CqrsNames names)
    {
        var folder = names.RequestKind == GeneratorKind.Query ? "Queries" : "Commands";
        var target = ResolveTarget(context, settings.Profile, "Tests", folder);
        var content = CqrsTemplates.Test(context, target.Namespace, names, settings);

        Add(files, target, $"{names.RequestType}Tests.cs", content);
    }

    private static (string Directory, string Namespace) ResolveTarget(
        GenerationContext context,
        CqrsProfile profile,
        params string[] folders)
    {
        var profileFolders = profile switch
        {
            CqrsProfile.Simple => Array.Empty<string>(),
            CqrsProfile.Standard => ["Application"],
            CqrsProfile.Clean => ["Application", "Features"],
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

        return GeneratorSupport.ResolveTarget(
            context,
            [.. profileFolders, .. folders]);
    }

    private static void Add(
        List<GeneratedFile> files,
        (string Directory, string Namespace) target,
        string fileName,
        string content)
    {
        files.Add(new GeneratedFile(Path.Combine(target.Directory, fileName), content));
    }
}

internal sealed record CqrsNames(
    GeneratorKind RequestKind,
    string RequestType,
    string HandlerType)
{
    public static CqrsNames Create(GeneratorKind kind, string name)
    {
        var requestKind = kind == GeneratorKind.Query
            ? GeneratorKind.Query
            : GeneratorKind.Command;

        var suffix = requestKind == GeneratorKind.Query ? "Query" : "Command";
        var requestType = name.EndsWith(suffix, StringComparison.Ordinal)
            ? name
            : name + suffix;

        if (kind == GeneratorKind.Handler)
        {
            if (name.EndsWith("QueryHandler", StringComparison.Ordinal))
            {
                requestKind = GeneratorKind.Query;
                requestType = name[..^"Handler".Length];
            }
            else if (name.EndsWith("CommandHandler", StringComparison.Ordinal))
            {
                requestKind = GeneratorKind.Command;
                requestType = name[..^"Handler".Length];
            }
            else if (name.EndsWith("Query", StringComparison.Ordinal))
            {
                requestKind = GeneratorKind.Query;
                requestType = name;
            }
            else if (name.EndsWith("Command", StringComparison.Ordinal))
            {
                requestKind = GeneratorKind.Command;
                requestType = name;
            }
            else
            {
                throw new CliException(
                    "Handler names must end with Command, Query, CommandHandler, or QueryHandler.");
            }
        }

        return new CqrsNames(requestKind, requestType, $"{requestType}Handler");
    }
}
