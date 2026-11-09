using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the code generation commands.
/// </summary>
internal static class MakeCommands
{
    public static IEnumerable<Command> Create(TextWriter output, TextWriter error)
    {
        yield return Create(
            "make:model",
            "Generates a model class.",
            GeneratorKind.Model,
            output,
            error);

        yield return Create(
            "make:service",
            "Generates a service class.",
            GeneratorKind.Service,
            output,
            error);

        yield return Create(
            "make:controller",
            "Generates an ASP.NET Core controller.",
            GeneratorKind.Controller,
            output,
            error);

        yield return Create(
            "make:resource",
            "Generates a model, a service, and a controller together.",
            GeneratorKind.Resource,
            output,
            error);
    }

    private static Command Create(
        string name,
        string description,
        GeneratorKind kind,
        TextWriter output,
        TextWriter error)
    {
        var arguments = new GeneratorArguments(
            new Argument<string>("name")
            {
                Description = "The name of the type to generate.",
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
                Description = "Places files directly in the output directory instead of convention folders.",
            });

        var command = new Command(name, description);

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);

        command.SetAction(
            parseResult => GeneratorRunner.Run(
                kind,
                parseResult,
                arguments,
                output,
                error));

        return command;
    }
}
