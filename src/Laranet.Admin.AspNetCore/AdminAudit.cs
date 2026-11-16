using Laranet.Admin;
using Microsoft.AspNetCore.Http;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Describes an administrative security event.
/// </summary>
public sealed record AdminAuditEvent(
    DateTimeOffset Timestamp,
    string? UserName,
    string? Resource,
    AdminOperation? Operation,
    string Action,
    string Outcome,
    string? IpAddress,
    string? Details = null);

/// <summary>
/// Receives optional administrative audit events.
/// </summary>
public interface IAdminAuditSink
{
    /// <summary>
    /// Records an administrative event.
    /// </summary>
    Task WriteAsync(
        AdminAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}

internal sealed class NullAdminAuditSink : IAdminAuditSink
{
    public Task WriteAsync(
        AdminAuditEvent auditEvent,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal static class AdminAudit
{
    public static async Task WriteSafeAsync(
        IAdminAuditSink sink,
        HttpContext context,
        string action,
        string outcome,
        AdminResource? resource = null,
        AdminOperation? operation = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await sink.WriteAsync(
                new AdminAuditEvent(
                    DateTimeOffset.UtcNow,
                    context.User.Identity?.Name,
                    resource?.Name,
                    operation,
                    action,
                    outcome,
                    context.Connection.RemoteIpAddress?.ToString(),
                    details),
                cancellationToken);
        }
        catch
        {
            // Auditing must not turn a successful administrative operation into a 500.
        }
    }
}
