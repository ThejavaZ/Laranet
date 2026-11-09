using System.Reflection;

namespace Laranet.Cli;

/// <summary>
/// Exposes the version of the running CLI, which matches the Laranet package version.
/// </summary>
internal static class LaranetVersion
{
    /// <summary>
    /// Gets the current Laranet package version.
    /// </summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var informationalVersion = typeof(LaranetVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "0.0.0";
        }

        var separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);

        return separator < 0
            ? informationalVersion
            : informationalVersion[..separator];
    }
}
