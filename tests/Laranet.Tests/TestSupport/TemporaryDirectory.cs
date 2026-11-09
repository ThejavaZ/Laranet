namespace Laranet.Tests.TestSupport;

/// <summary>
/// Creates an isolated temporary directory for a test.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Root = Directory.CreateTempSubdirectory("laranet-cli-tests-").FullName;
    }

    public string Root { get; }

    public string GetPath(params string[] segments)
    {
        return Path.Combine([Root, .. segments]);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup for temporary test data.
        }
    }
}
