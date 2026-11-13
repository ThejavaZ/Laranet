namespace Laranet.Cli.Generation;

/// <summary>
/// Identifies the responsibility of a project inside a multi-project template.
/// </summary>
internal enum ProjectLayerRole
{
    Domain,
    Application,
    Infrastructure,
    Host,
}

/// <summary>
/// Describes one project inside a multi-project template.
/// </summary>
/// <param name="FolderName">The project folder and project file name.</param>
/// <param name="RootNamespace">The root namespace of the project.</param>
/// <param name="Sdk">The MSBuild SDK used by the project.</param>
/// <param name="Role">The responsibility of the project.</param>
/// <param name="ProjectReferences">The folder names of the projects referenced by this project.</param>
internal sealed record ProjectLayer(
    string FolderName,
    string RootNamespace,
    string Sdk,
    ProjectLayerRole Role,
    IReadOnlyList<string> ProjectReferences)
{
    /// <summary>
    /// Gets a value indicating whether the project is the runnable host.
    /// </summary>
    public bool IsHost => Role == ProjectLayerRole.Host;
}
