using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Prints the available commands grouped by family.
/// </summary>
internal static class ListCommand
{
    private static readonly (string Group, string[] Commands)[] Groups =
    [
        ("Proyecto y generación", new[]
        {
            "new",
            "make:model",
            "make:service",
            "make:controller",
            "make:middleware",
            "make:all",
            "make:resource",
            "make:crud",
            "make:repository",
            "make:command",
            "make:query",
            "make:handler",
            "make:config",
            "make:admin-resource",
        }),
        ("Base de datos", new[]
        {
            "make:dbcontext",
        }),
        ("Diagnóstico", new[]
        {
            "doctor",
        }),
        ("Información", new[]
        {
            "list",
            "--help",
            "--version",
        }),
    ];

    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.Ordinal)
    {
        ["new"] = "Crea un proyecto Laranet.",
        ["make:model"] = "Genera una clase de modelo.",
        ["make:service"] = "Genera una clase de servicio.",
        ["make:controller"] = "Genera un controlador ASP.NET Core.",
        ["make:middleware"] = "Genera un middleware ASP.NET Core.",
        ["make:all"] = "Genera modelo, servicio y controlador juntos.",
        ["make:resource"] = "Genera un view model API Resource.",
        ["make:crud"] = "Genera una entidad, DTOs y endpoints CRUD.",
        ["make:repository"] = "Genera un scaffold opcional de repositorio.",
        ["make:command"] = "Genera un comando, opcionalmente con handler.",
        ["make:query"] = "Genera una consulta, opcionalmente con handler.",
        ["make:handler"] = "Genera un handler para un comando o consulta.",
        ["make:config"] = "Genera opciones y registro de configuración.",
        ["make:admin-resource"] = "Genera una clase AdminResource para una entidad.",
        ["make:dbcontext"] = "Genera un DbContext, configuraciones y factory.",
        ["doctor"] = "Verifica dependencias, versiones y riesgos del proyecto.",
        ["list"] = "Muestra los comandos disponibles.",
        ["--help"] = "Muestra ayuda e información de uso.",
        ["--version"] = "Muestra la versión instalada.",
    };

    public static Command Create(TextWriter output)
    {
        var command = new Command("list", "Lists the available commands.");

        command.SetAction(_ =>
        {
            CliStyle.WriteHeader(output, "Comandos disponibles:");
            output.WriteLine();

            var maxName = Groups
                .SelectMany(group => group.Commands)
                .Max(name => name.Length);

            foreach (var (group, commands) in Groups)
            {
                CliStyle.WriteHeader(output, group);
                output.WriteLine();

                foreach (var name in commands)
                {
                    var description = Descriptions.TryGetValue(name, out var value)
                        ? value
                        : string.Empty;
                    var padded = name.PadRight(maxName);

                    output.Write("  ");
                    CliStyle.WriteCommand(output, padded);
                    output.Write("  ");
                    output.WriteLine(description);
                }

                output.WriteLine();
            }

            CliStyle.WriteNextStep(output, "laranet <comando> --help");
            return 0;
        });

        return command;
    }
}
