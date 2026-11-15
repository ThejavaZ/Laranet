using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Laranet.Cli.Doctor;

internal sealed class DoctorAnalyzer
{
    private static readonly Regex TypeDeclaration = new(
        @"^(?:(?:public|internal|private|protected|file|static|sealed|abstract|partial)\s+)*(?:class|record|interface|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<generic><[^>]+>)?",
        RegexOptions.Compiled);
    private static readonly Regex NamespaceDeclaration = new(
        @"^\s*namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)",
        RegexOptions.Compiled);
    private static readonly Regex PropertyType = new(
        @"\bpublic\s+(?:required\s+)?(?<type>[A-Za-z_][A-Za-z0-9_.]*(?:\?)?(?:\[\])?)\s+\w+\s*\{",
        RegexOptions.Compiled);

    public DoctorReport Analyze(DoctorOptions options)
    {
        var diagnostics = new List<DoctorDiagnostic>();
        var projects = DiscoverProjects(options.RootPath);

        CheckProjectReferences(projects, diagnostics);
        CheckVersions(projects, diagnostics);
        CheckDuplicateTypes(options.RootPath, diagnostics);

        if (options.CheckPagination)
        {
            CheckPagination(options.RootPath, diagnostics);
        }

        if (options.CheckControllerDbContext)
        {
            CheckControllerDbContext(options.RootPath, diagnostics);
        }

        if (options.CheckDtoEntities)
        {
            CheckDtoEntities(options.RootPath, diagnostics);
        }

        var errors = diagnostics.Count(diagnostic => diagnostic.Severity == DoctorSeverity.Error);
        var warnings = diagnostics.Count - errors;
        var exitCode = errors > 0 || options.Strict && warnings > 0 ? 1 : 0;

        return new DoctorReport(
            diagnostics
                .OrderBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Line)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ToArray(),
            errors,
            warnings,
            exitCode);
    }

    private static IReadOnlyList<DoctorProject> DiscoverProjects(string rootPath)
    {
        var projectFiles = Directory
            .EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildDirectory(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var centralVersions = ReadCentralVersions(rootPath);

        return projectFiles
            .Select(path => ReadProject(path, centralVersions))
            .ToArray();
    }

    private static DoctorProject ReadProject(
        string path,
        IReadOnlyDictionary<string, string> centralVersions)
    {
        var document = XDocument.Load(path);
        var projectDirectory = Path.GetDirectoryName(path)!;
        var references = document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => Path.GetFullPath(
                Path.Combine(projectDirectory, (string?)element.Attribute("Include") ?? string.Empty)))
            .ToArray();
        var packages = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element =>
            {
                var name = (string?)element.Attribute("Include") ?? string.Empty;
                var version = (string?)element.Attribute("Version")
                    ?? centralVersions.GetValueOrDefault(name, string.Empty);
                return (name, version);
            })
            .Where(package => package.name.Length > 0)
            .ToDictionary(
                package => package.name,
                package => package.version,
                StringComparer.OrdinalIgnoreCase);
        var frameworks = document
            .Descendants()
            .Where(element =>
                element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .SelectMany(element => element.Value.Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new DoctorProject(path, references, packages, frameworks);
    }

    private static IReadOnlyDictionary<string, string> ReadCentralVersions(string rootPath)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var directory = new DirectoryInfo(rootPath);

        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "Directory.Packages.props");

            if (File.Exists(path))
            {
                var document = XDocument.Load(path);

                foreach (var element in document.Descendants().Where(
                             item => item.Name.LocalName == "PackageVersion"))
                {
                    var name = (string?)element.Attribute("Include");
                    var version = (string?)element.Attribute("Version");

                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                    {
                        versions[name] = version;
                    }
                }

                break;
            }

            directory = directory.Parent;
        }

        return versions;
    }

    private static void CheckProjectReferences(
        IReadOnlyList<DoctorProject> projects,
        List<DoctorDiagnostic> diagnostics)
    {
        var knownProjects = projects
            .Select(project => project.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var graph = projects.ToDictionary(
            project => project.Path,
            project => project.References.Where(knownProjects.Contains).ToArray(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            foreach (var reference in project.References.Where(reference => !knownProjects.Contains(reference)))
            {
                diagnostics.Add(new DoctorDiagnostic(
                    "LARANET001",
                    DoctorSeverity.Error,
                    $"Project reference does not exist: {reference}.",
                    project.Path));
            }
        }

        var states = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();

        foreach (var project in projects)
        {
            Visit(project.Path);
        }

        void Visit(string project)
        {
            if (states.GetValueOrDefault(project) == 2)
            {
                return;
            }

            if (states.GetValueOrDefault(project) == 1)
            {
                var start = stack.FindIndex(path =>
                    string.Equals(path, project, StringComparison.OrdinalIgnoreCase));
                var cycle = string.Join(
                    " -> ",
                    stack[start..].Append(project).Select(Path.GetFileName));

                diagnostics.Add(new DoctorDiagnostic(
                    "LARANET002",
                    DoctorSeverity.Error,
                    $"Circular project reference detected: {cycle}.",
                    project));
                return;
            }

            states[project] = 1;
            stack.Add(project);

            foreach (var reference in graph.GetValueOrDefault(project) ?? [])
            {
                Visit(reference);
            }

            stack.RemoveAt(stack.Count - 1);
            states[project] = 2;
        }
    }

    private static void CheckVersions(
        IReadOnlyList<DoctorProject> projects,
        List<DoctorDiagnostic> diagnostics)
    {
        foreach (var project in projects)
        {
            foreach (var framework in project.TargetFrameworks)
            {
                if (!string.Equals(framework, "net10.0", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(new DoctorDiagnostic(
                        "LARANET003",
                        DoctorSeverity.Error,
                        $"Unsupported target framework '{framework}'. Supported framework: net10.0.",
                        project.Path));
                }
            }

            foreach (var package in project.Packages)
            {
                if (package.Key.StartsWith("Laranet", StringComparison.OrdinalIgnoreCase)
                    && !package.Value.StartsWith("0.2.", StringComparison.Ordinal))
                {
                    diagnostics.Add(new DoctorDiagnostic(
                        "LARANET004",
                        DoctorSeverity.Error,
                        $"Unsupported Laranet package version '{package.Value}' for {package.Key}. Supported line: 0.2.x.",
                        project.Path));
                }

                if (package.Key.StartsWith(
                        "Microsoft.EntityFrameworkCore",
                        StringComparison.OrdinalIgnoreCase)
                    && !package.Value.StartsWith("10.", StringComparison.Ordinal))
                {
                    diagnostics.Add(new DoctorDiagnostic(
                        "LARANET005",
                        DoctorSeverity.Error,
                        $"Unsupported EF Core version '{package.Value}' for {package.Key}. Supported major version: 10.",
                        project.Path));
                }
            }
        }
    }

    private static void CheckDuplicateTypes(
        string rootPath,
        List<DoctorDiagnostic> diagnostics)
    {
        var declarations = new Dictionary<string, List<(string Path, int Line)>>();

        foreach (var file in SourceFiles(rootPath))
        {
            var content = File.ReadAllText(file);

            foreach (var declaration in Declarations(content))
            {
                var key = $"{declaration.Namespace}::{declaration.Name}";

                declarations.TryAdd(key, []);
                declarations[key].Add((file, declaration.Line));
            }
        }

        foreach (var declaration in declarations.Where(item => item.Value.Count > 1))
        {
            var typeName = declaration.Key[(declaration.Key.IndexOf("::", StringComparison.Ordinal) + 2)..];

            diagnostics.Add(new DoctorDiagnostic(
                "LARANET006",
                DoctorSeverity.Error,
                $"Type '{typeName}' is declared in multiple files: " +
                string.Join(", ", declaration.Value.Select(item => Path.GetFileName(item.Path))),
                declaration.Value[0].Path,
                declaration.Value[0].Line));
        }
    }

    private static void CheckPagination(
        string rootPath,
        List<DoctorDiagnostic> diagnostics)
    {
        foreach (var file in SourceFiles(rootPath))
        {
            var content = File.ReadAllText(file);

            if (content.Contains("MapGet(", StringComparison.Ordinal)
                && (content.Contains("ToListAsync(", StringComparison.Ordinal)
                    || content.Contains("ToList(", StringComparison.Ordinal))
                && !content.Contains("Paginate", StringComparison.Ordinal)
                && !content.Contains(".Take(", StringComparison.Ordinal))
            {
                diagnostics.Add(new DoctorDiagnostic(
                    "LARANET007",
                    DoctorSeverity.Warning,
                    "Endpoint appears to materialize a query without pagination or a Take limit.",
                    file));
            }
        }
    }

    private static void CheckControllerDbContext(
        string rootPath,
        List<DoctorDiagnostic> diagnostics)
    {
        foreach (var file in SourceFiles(rootPath).Where(IsController))
        {
            var content = File.ReadAllText(file);

            if (content.Contains("DbContext", StringComparison.Ordinal))
            {
                diagnostics.Add(new DoctorDiagnostic(
                    "LARANET008",
                    DoctorSeverity.Warning,
                    "Controller directly references DbContext. Consider moving persistence access to an application service.",
                    file));
            }
        }
    }

    private static void CheckDtoEntities(
        string rootPath,
        List<DoctorDiagnostic> diagnostics)
    {
        var entityNames = SourceFiles(rootPath)
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}Models{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => Declarations(File.ReadAllText(file)))
            .Select(declaration => declaration.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var file in SourceFiles(rootPath).Where(IsDto))
        {
            var content = File.ReadAllText(file);

            foreach (Match match in PropertyType.Matches(content))
            {
                var type = match.Groups["type"].Value.TrimEnd('?', '[', ']');

                if (entityNames.Contains(type))
                {
                    diagnostics.Add(new DoctorDiagnostic(
                        "LARANET009",
                        DoctorSeverity.Warning,
                        $"DTO exposes entity type '{type}' directly.",
                        file));
                    break;
                }
            }
        }
    }

    private static IEnumerable<string> SourceFiles(string rootPath)
    {
        return Directory
            .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildDirectory(file))
            .OrderBy(file => file, StringComparer.Ordinal);
    }

    private static IEnumerable<TypeDeclarationInfo> Declarations(string content)
    {
        var namespaceName = string.Empty;
        var inRawString = false;
        var lines = content.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');

            if (inRawString)
            {
                if (line.Contains("\"\"\"", StringComparison.Ordinal))
                {
                    inRawString = false;
                }

                continue;
            }

            if (line.Contains("\"\"\"", StringComparison.Ordinal))
            {
                inRawString = true;

                continue;
            }

            var namespaceMatch = NamespaceDeclaration.Match(line);

            if (namespaceMatch.Success)
            {
                namespaceName = namespaceMatch.Groups["name"].Value;
            }

            var typeMatch = TypeDeclaration.Match(line);

            if (typeMatch.Success)
            {
                yield return new TypeDeclarationInfo(
                    namespaceName,
                    typeMatch.Groups["name"].Value + typeMatch.Groups["generic"].Value,
                    index + 1);
            }
        }
    }

    private static bool IsController(string path)
    {
        return Path.GetFileName(path).EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDto(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);

        return name.EndsWith("Dto", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Request", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Response", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBuildDirectory(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj" or ".git");
    }

    private sealed record TypeDeclarationInfo(string Namespace, string Name, int Line);
}
