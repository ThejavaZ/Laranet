using System.CommandLine;
using Laranet.Cli.Commands;

namespace Laranet.Cli;

/// <summary>
/// Builds and executes the command-line application.
/// </summary>
internal static class CliApplication
{
    public static RootCommand CreateRootCommand(
        TextWriter? output = null,
        TextWriter? error = null)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        var rootCommand = new RootCommand(
            "Creates Laranet projects and generates application code from the command line.");

        rootCommand.Subcommands.Add(NewCommand.Create(output, error));

        foreach (var command in MakeCommands.Create(output, error))
        {
            rootCommand.Subcommands.Add(command);
        }

        return rootCommand;
    }

    public static int Execute(
        string[] args,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        var configuration = new InvocationConfiguration
        {
            Output = output ?? Console.Out,
            Error = error ?? Console.Error,
        };

        return CreateRootCommand(output, error)
            .Parse(args)
            .Invoke(configuration);
    }
}
