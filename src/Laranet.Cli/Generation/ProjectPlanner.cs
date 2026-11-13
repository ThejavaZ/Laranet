namespace Laranet.Cli.Generation;

/// <summary>
/// Builds the file plan for a new project.
/// </summary>
internal static class ProjectPlanner
{
    /// <summary>
    /// Creates the plan for a new project.
    /// </summary>
    /// <param name="projectName">The validated project folder name.</param>
    /// <param name="rootNamespace">The root namespace of the generated project.</param>
    /// <param name="projectDirectory">The absolute project or repository directory.</param>
    /// <param name="template">The project template.</param>
    /// <param name="layout">The directory structure to use.</param>
    /// <param name="includeSolution">Whether to create a solution in the project layout.</param>
    /// <param name="packages">The Laranet packages to reference.</param>
    /// <returns>The generation plan.</returns>
    public static GenerationPlan Create(
        string projectName,
        string rootNamespace,
        string projectDirectory,
        ProjectTemplate template,
        ProjectLayout layout,
        bool includeSolution,
        IReadOnlyList<string> packages)
    {
        return ProjectTemplates.IsMultiProject(template)
            ? CreateMultiProject(
                projectName,
                rootNamespace,
                projectDirectory,
                template,
                packages)
            : CreateSingleProject(
                projectName,
                rootNamespace,
                projectDirectory,
                template,
                layout,
                includeSolution,
                packages);
    }

    private static GenerationPlan CreateSingleProject(
        string projectName,
        string rootNamespace,
        string projectDirectory,
        ProjectTemplate template,
        ProjectLayout layout,
        bool includeSolution,
        IReadOnlyList<string> packages)
    {
        var isRepository = layout == ProjectLayout.Repository;
        var sourceDirectory = isRepository
            ? Path.Combine(projectDirectory, "src", projectName)
            : projectDirectory;
        var runCommand = isRepository
            ? $"dotnet run --project src/{projectName}/{projectName}.csproj"
            : "dotnet run";

        var files = new List<GeneratedFile>
        {
            new(
                Path.Combine(sourceDirectory, $"{projectName}.csproj"),
                ProjectTemplates.ProjectFile(template, rootNamespace, packages)),
        };

        foreach (var (relativePath, content) in ProjectTemplates.SingleProjectContents(
            template,
            projectName,
            rootNamespace))
        {
            files.Add(new GeneratedFile(
                Path.Combine(sourceDirectory, relativePath),
                content));
        }

        files.Add(new GeneratedFile(
            Path.Combine(projectDirectory, "README.md"),
            ProjectTemplates.Readme(projectName, runCommand)));
        files.Add(new GeneratedFile(
            Path.Combine(projectDirectory, ".gitignore"),
            ProjectTemplates.GitIgnore()));

        if (template != ProjectTemplate.Console)
        {
            AddWebSettings(files, sourceDirectory);
        }

        if (includeSolution || isRepository)
        {
            var solutionDirectory = isRepository
                ? projectDirectory
                : Path.GetDirectoryName(projectDirectory) ?? projectDirectory;
            var projectPath = isRepository
                ? $"src/{projectName}/{projectName}.csproj"
                : $"{projectName}/{projectName}.csproj";

            files.Add(new GeneratedFile(
                Path.Combine(solutionDirectory, $"{projectName}.slnx"),
                ProjectTemplates.Solution([projectPath])));
        }

        return new GenerationPlan(projectName, files);
    }

    private static GenerationPlan CreateMultiProject(
        string projectName,
        string rootNamespace,
        string projectDirectory,
        ProjectTemplate template,
        IReadOnlyList<string> packages)
    {
        var layers = ProjectTemplates.Layers(template, projectName, rootNamespace);
        var files = new List<GeneratedFile>();

        foreach (var layer in layers)
        {
            var sourceDirectory = Path.Combine(
                projectDirectory,
                "src",
                layer.FolderName);

            files.Add(new GeneratedFile(
                Path.Combine(sourceDirectory, $"{layer.FolderName}.csproj"),
                ProjectTemplates.LayerProjectFile(
                    layer,
                    layer.IsHost ? packages : [])));

            foreach (var (relativePath, content) in ProjectTemplates.LayerContents(
                template,
                rootNamespace,
                layer))
            {
                files.Add(new GeneratedFile(
                    Path.Combine(sourceDirectory, relativePath),
                    content));
            }

            if (layer.IsHost)
            {
                AddWebSettings(files, sourceDirectory);
            }
        }

        var hostProjectPath = ProjectTemplates.HostProjectPath(template, projectName)!;

        files.Add(new GeneratedFile(
            Path.Combine(projectDirectory, "README.md"),
            ProjectTemplates.Readme(
                projectName,
                $"dotnet run --project {hostProjectPath}",
                layers)));
        files.Add(new GeneratedFile(
            Path.Combine(projectDirectory, ".gitignore"),
            ProjectTemplates.GitIgnore()));
        files.Add(new GeneratedFile(
            Path.Combine(projectDirectory, $"{projectName}.slnx"),
            ProjectTemplates.Solution(
                layers.Select(layer =>
                    $"src/{layer.FolderName}/{layer.FolderName}.csproj"))));

        return new GenerationPlan(projectName, files);
    }

    private static void AddWebSettings(
        List<GeneratedFile> files,
        string sourceDirectory)
    {
        files.Add(new GeneratedFile(
            Path.Combine(sourceDirectory, "appsettings.json"),
            ProjectTemplates.AppSettings()));
        files.Add(new GeneratedFile(
            Path.Combine(sourceDirectory, "appsettings.Development.json"),
            ProjectTemplates.AppSettingsDevelopment()));
        files.Add(new GeneratedFile(
            Path.Combine(sourceDirectory, "Properties", "launchSettings.json"),
            ProjectTemplates.LaunchSettings()));
    }
}
