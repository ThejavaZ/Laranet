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
    /// <param name="projectDirectory">The absolute project directory.</param>
    /// <param name="template">The project template.</param>
    /// <param name="packages">The Laranet packages to reference.</param>
    /// <returns>The generation plan.</returns>
    public static GenerationPlan Create(
        string projectName,
        string rootNamespace,
        string projectDirectory,
        ProjectTemplate template,
        IReadOnlyList<string> packages)
    {
        var files = new List<GeneratedFile>
        {
            new(
                Path.Combine(projectDirectory, $"{projectName}.csproj"),
                ProjectTemplates.ProjectFile(
                    template,
                    rootNamespace,
                    packages)),
            new(
                Path.Combine(projectDirectory, "Program.cs"),
                ProjectTemplates.Program(rootNamespace, template)),
            new(
                Path.Combine(projectDirectory, ".gitignore"),
                ProjectTemplates.GitIgnore()),
            new(
                Path.Combine(projectDirectory, "README.md"),
                ProjectTemplates.Readme(projectName)),
        };

        if (template == ProjectTemplate.WebApi)
        {
            files.Add(new GeneratedFile(
                Path.Combine(projectDirectory, "appsettings.json"),
                ProjectTemplates.AppSettings()));
            files.Add(new GeneratedFile(
                Path.Combine(projectDirectory, "appsettings.Development.json"),
                ProjectTemplates.AppSettingsDevelopment()));
            files.Add(new GeneratedFile(
                Path.Combine(projectDirectory, "Properties", "launchSettings.json"),
                ProjectTemplates.LaunchSettings()));
        }

        return new GenerationPlan(projectName, files);
    }
}
