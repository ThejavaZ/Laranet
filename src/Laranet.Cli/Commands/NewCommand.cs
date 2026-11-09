using System.CommandLine;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the project creation command.
/// </summary>
internal static class NewCommand
{
    public static Command Create(TextWriter output, TextWriter error)
    {
        var arguments = new NewProjectArguments(
            new Argument<string>("name")
            {
                Description = "The project name, also used as the folder name.",
            },
            new Option<string>("--output", "-o")
            {
                Description = "The directory where the project folder is created. Defaults to the current directory.",
                DefaultValueFactory = _ => ".",
            },
            new Option<string?>("--namespace", "-n")
            {
                Description = "The root namespace. Derived from the project name by default.",
            },
            new Option<string?>("--template", "-t")
            {
                Description = "The project template: webapi (default) or console.",
            },
            new Option<bool>("--with-data")
            {
                Description = "References the Laranet.Data package.",
            },
            new Option<bool>("--with-efcore")
            {
                Description = "References the Laranet.EntityFrameworkCore package.",
            },
            new Option<bool>("--with-aspnetcore")
            {
                Description = "References the Laranet.AspNetCore package.",
            },
            new Option<bool>("--force", "-f")
            {
                Description = "Writes into an existing non-empty directory and overwrites generated files.",
            },
            new Option<bool>("--dry-run")
            {
                Description = "Shows the files that would be generated without writing them.",
            });

        var command = new Command("new", "Creates a new Laranet project.");

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Template);
        command.Options.Add(arguments.WithData);
        command.Options.Add(arguments.WithEfCore);
        command.Options.Add(arguments.WithAspNetCore);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);

        command.SetAction(
            parseResult => NewProjectRunner.Run(
                parseResult,
                arguments,
                output,
                error));

        return command;
    }
}
