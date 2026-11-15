using System.Text.Json.Serialization;

namespace Laranet.Cli.Doctor;

internal enum DoctorSeverity
{
    Warning,
    Error,
}

internal sealed record DoctorDiagnostic(
    string Code,
    DoctorSeverity Severity,
    string Message,
    string? Path = null,
    int? Line = null);

internal sealed record DoctorReport(
    IReadOnlyList<DoctorDiagnostic> Diagnostics,
    int ErrorCount,
    int WarningCount,
    int ExitCode)
{
    public bool HasErrors => ErrorCount > 0;
}

internal sealed record DoctorOptions(
    string RootPath,
    bool Json,
    bool Strict,
    bool CheckPagination,
    bool CheckControllerDbContext,
    bool CheckDtoEntities);

internal sealed record DoctorProject(
    string Path,
    IReadOnlyList<string> References,
    IReadOnlyDictionary<string, string> Packages,
    IReadOnlyList<string> TargetFrameworks);

internal static class DoctorJson
{
    public static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };
}
