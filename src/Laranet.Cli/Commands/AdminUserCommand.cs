using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the admin user seeder generation command.
/// </summary>
internal static class AdminUserCommand
{
    public static Command Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new AdminUserArguments(
            new Argument<string?>("name")
            {
                Description = "The seeder class name.",
                Arity = ArgumentArity.ZeroOrOne,
            },
            new Option<string>("--output", "-o")
            {
                Description = "The output directory. Defaults to the current directory.",
                DefaultValueFactory = _ => ".",
            },
            new Option<string?>("--namespace", "-n")
            {
                Description = "The namespace for generated code. Inferred from the nearest project by default.",
            },
            new Option<bool>("--force", "-f")
            {
                Description = "Overwrites existing files.",
            },
            new Option<bool>("--dry-run")
            {
                Description = "Shows the files that would be generated without writing them.",
            },
            new Option<bool>("--flat")
            {
                Description = "Places files directly in the output directory.",
            },
            new Option<bool>("--interactive", "-i")
            {
                Description = "Prompts for missing values and overwrite confirmation.",
            });

        var command = new Command(
            "admin:user",
            "Generates a seeder for the first admin user.");

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        command.SetAction(
            parseResult => AdminUserRunner.Run(
                parseResult,
                arguments,
                input,
                output,
                error));

        return command;
    }
}

/// <summary>
/// Holds the command-line symbols used by <c>admin:user</c>.
/// </summary>
internal sealed record AdminUserArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Flat,
    Option<bool> Interactive) : GeneratorArguments(
        Name,
        Output,
        Namespace,
        Force,
        DryRun,
        Flat,
        Interactive);
