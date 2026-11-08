using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Laranet.Tests.Integration.Data.TestSupport;

/// <summary>
/// Captures the commands executed through Entity Framework Core so tests can inspect
/// generated SQL and parameter values.
/// </summary>
internal sealed class CommandCaptureInterceptor : DbCommandInterceptor
{
    private readonly List<CapturedCommand> _commands = [];

    public IReadOnlyList<CapturedCommand> Commands => _commands;

    public void Clear() => _commands.Clear();

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Capture(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return base.ReaderExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Capture(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return base.ScalarExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return base.NonQueryExecutingAsync(
            command,
            eventData,
            result,
            cancellationToken);
    }

    private void Capture(DbCommand command)
    {
        var parameters = command.Parameters
            .Cast<DbParameter>()
            .Select(parameter => new CapturedParameter(
                parameter.ParameterName,
                parameter.Value))
            .ToArray();

        _commands.Add(new CapturedCommand(command.CommandText, parameters));
    }
}

internal sealed record CapturedCommand(
    string CommandText,
    IReadOnlyList<CapturedParameter> Parameters);

internal sealed record CapturedParameter(string Name, object? Value);
