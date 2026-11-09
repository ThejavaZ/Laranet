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
        var sourceDirectory = layout == ProjectLayout.Repository
            ? Path.Combine(projectDirectory, "src", projectName)
            : projectDirectory;

        var files = new List<GeneratedFile>
        {
            new(
                Path.Combine(sourceDirectory, $"{projectName}.csproj"),
                ProjectTemplates.ProjectFile(
                    template,
                    rootNamespace,
                    packages)),
            new(
                Path.Combine(sourceDirectory, "Program.cs"),
                ProjectTemplates.Program(rootNamespace, template)),
            new(
                Path.Combine(projectDirectory, "README.md"),
                layout == ProjectLayout.Repository
                    ? ProjectTemplates.RepositoryReadme(projectName)
                    : ProjectTemplates.Readme(projectName)),
            new(
                Path.Combine(projectDirectory, ".gitignore"),
                ProjectTemplates.GitIgnore()),
        };

        if (template == ProjectTemplate.WebApi)
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

        if (includeSolution || layout == ProjectLayout.Repository)
        {
            var isRepository = layout == ProjectLayout.Repository;
            var solutionDirectory = isRepository
                ? projectDirectory
                : Path.GetDirectoryName(projectDirectory) ?? projectDirectory;
            var projectPath = isRepository
                ? $"src/{projectName}/{projectName}.csproj"
                : $"{projectName}/{projectName}.csproj";

            files.Add(new GeneratedFile(
                Path.Combine(solutionDirectory, $"{projectName}.slnx"),
                ProjectTemplates.Solution(projectPath)));
        }

        return new GenerationPlan(projectName, files);
    }
}
