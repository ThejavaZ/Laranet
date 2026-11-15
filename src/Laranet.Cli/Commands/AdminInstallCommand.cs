using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the admin installation command.
/// </summary>
internal static class AdminInstallCommand
{
    public static Command Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "The output directory. Defaults to the current directory.",
            DefaultValueFactory = _ => ".",
        };
        var namespaceOption = new Option<string?>("--namespace", "-n")
        {
            Description = "The namespace for generated code. Inferred from the nearest project by default.",
        };
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Overwrites existing files.",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Shows the files that would be generated without writing them.",
        };

        var command = new Command(
            "admin:install",
            "Installs the Laranet admin panel in the current application.");

        command.Options.Add(outputOption);
        command.Options.Add(namespaceOption);
        command.Options.Add(forceOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            parseResult => AdminInstallRunner.Run(
                parseResult,
                outputOption,
                namespaceOption,
                forceOption,
                dryRunOption,
                output,
                error));

        return command;
    }
}
