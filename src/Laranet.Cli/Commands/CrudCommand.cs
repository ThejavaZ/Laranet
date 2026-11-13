using System.CommandLine;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the CRUD generation command.
/// </summary>
internal static class CrudCommand
{
    public static Command Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new CrudArguments(
            new Argument<string?>("name")
            {
                Description = "The entity type to generate.",
                Arity = ArgumentArity.ZeroOrOne,
            },
            new Option<string>("--output", "-o")
            {
                Description = "The output directory. Defaults to the current directory.",
                DefaultValueFactory = _ => ".",
            },
            new Option<string?>("--namespace", "-n")
            {
                Description = "The base namespace for generated code. Inferred from the nearest project by default.",
            },
            new Option<string?>("--profile", "-p")
            {
                Description = "The CRUD profile: simple (default), standard, clean, or modular.",
            },
            new Option<string?>("--context", "-c")
            {
                Description = "The fully qualified DbContext type. Discovered from the nearest project by default.",
            },
            new Option<string?>("--fields")
            {
                Description = "The entity fields as Name:type pairs separated by commas. Defaults to Name:string.",
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
                Description = "Places files directly in the output directory instead of convention folders.",
            },
            new Option<bool>("--interactive", "-i")
            {
                Description = "Prompts for missing values and overwrite confirmation.",
            });

        var command = new Command(
            "make:crud",
            "Generates an entity, DTOs, and CRUD endpoints for an entity.");

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Profile);
        command.Options.Add(arguments.Context);
        command.Options.Add(arguments.Fields);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        command.SetAction(
            parseResult => CrudRunner.Run(
                parseResult,
                arguments,
                input,
                output,
                error));

        return command;
    }
}

/// <summary>
/// Holds the command-line symbols used by <c>make:crud</c>.
/// </summary>
internal sealed record CrudArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string?> Profile,
    Option<string?> Context,
    Option<string?> Fields,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Flat,
    Option<bool> Interactive);
