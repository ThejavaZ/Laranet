using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the code generation commands.
/// </summary>
internal static class MakeCommands
{
    public static IEnumerable<Command> Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        yield return Create(
            "make:model",
            "Generates a model class.",
            GeneratorKind.Model,
            output,
            error,
            input);

        yield return Create(
            "make:service",
            "Generates a service class.",
            GeneratorKind.Service,
            output,
            error,
            input);

        yield return Create(
            "make:controller",
            "Generates an ASP.NET Core controller.",
            GeneratorKind.Controller,
            output,
            error,
            input);

        yield return Create(
            "make:middleware",
            "Generates an ASP.NET Core middleware.",
            GeneratorKind.Middleware,
            output,
            error,
            input);

        yield return Create(
            "make:resource",
            "Generates a model, a service, and a controller together.",
            GeneratorKind.Resource,
            output,
            error,
            input);

        yield return Create(
            "make:repository",
            "Generates an optional repository scaffold. Use it only when the abstraction is justified.",
            GeneratorKind.Repository,
            output,
            error,
            input);
    }

    private static Command Create(
        string name,
        string description,
        GeneratorKind kind,
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new GeneratorArguments(
            new Argument<string?>("name")
            {
                Description = "The name of the type to generate.",
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
                Description = "Places files directly in the output directory instead of convention folders.",
            },
            new Option<bool>("--interactive", "-i")
            {
                Description = "Prompts for missing values and overwrite confirmation.",
            });

        var command = new Command(name, description);

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        command.SetAction(
            parseResult => GeneratorRunner.Run(
                kind,
                parseResult,
                arguments,
                input,
                output,
                error));

        return command;
    }
}
