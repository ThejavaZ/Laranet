namespace Laranet.Cli.Generation;

/// <summary>
/// Contains the file contents used to create new projects.
/// </summary>
internal static class ProjectTemplates
{
    private const string WebApiSdk = "Microsoft.NET.Sdk.Web";
    private const string ConsoleSdk = "Microsoft.NET.Sdk";

    private const string WebApiProgramTemplate = """
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.MapGet("/", () => Results.Ok(new { Status = "running" }));
        app.MapGet("/health", () => Results.Ok(new { Status = "healthy" }));

        app.Run();

        """;

    private const string ConsoleProgramTemplate = """
        Console.WriteLine("{{Namespace}} is ready.");

        """;

    private const string AppSettingsTemplate = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "AllowedHosts": "*"
        }

        """;

    private const string AppSettingsDevelopmentTemplate = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Information"
            }
          }
        }

        """;

    private const string LaunchSettingsTemplate = """
        {
          "$schema": "https://json.schemastore.org/launchsettings.json",
          "profiles": {
            "http": {
              "commandName": "Project",
              "dotnetRunMessages": true,
              "launchBrowser": false,
              "applicationUrl": "http://localhost:5080",
              "environmentVariables": {
                "ASPNETCORE_ENVIRONMENT": "Development"
              }
            }
          }
        }

        """;

    private const string GitIgnoreTemplate = """
        bin/
        obj/
        .vs/
        *.user

        """;

    private const string ReadmeTemplate = """
        # {{ProjectName}}

        Project generated with [Laranet](https://github.com/TheJavaZ/Laranet).

        ## Getting started

        ```bash
        dotnet run
        ```

        """;

    /// <summary>
    /// Parses a template name supplied on the command line.
    /// </summary>
    /// <param name="value">The template name, or <see langword="null"/> for the default.</param>
    /// <returns>The parsed template.</returns>
    /// <exception cref="CliException">The template name is not known.</exception>
    public static ProjectTemplate Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ProjectTemplate.WebApi;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "webapi" or "web-api" => ProjectTemplate.WebApi,
            "console" => ProjectTemplate.Console,
            _ => throw new CliException(
                $"'{value}' is not a known template. Use 'webapi' or 'console'."),
        };
    }

    /// <summary>
    /// Gets the SDK used by a template.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <returns>The SDK name.</returns>
    public static string Sdk(ProjectTemplate template)
    {
        return template switch
        {
            ProjectTemplate.WebApi => WebApiSdk,
            ProjectTemplate.Console => ConsoleSdk,
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null),
        };
    }

    /// <summary>
    /// Generates a project file.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <param name="packages">The Laranet packages to reference.</param>
    /// <returns>The project file content.</returns>
    public static string ProjectFile(
        ProjectTemplate template,
        string rootNamespace,
        IReadOnlyList<string> packages)
    {
        var lines = new List<string>
        {
            $"<Project Sdk=\"{Sdk(template)}\">",
            "",
            "  <PropertyGroup>",
            "    <TargetFramework>net10.0</TargetFramework>",
        };

        if (template == ProjectTemplate.Console)
        {
            lines.Add("    <OutputType>Exe</OutputType>");
        }

        lines.Add("    <Nullable>enable</Nullable>");
        lines.Add("    <ImplicitUsings>enable</ImplicitUsings>");
        lines.Add($"    <RootNamespace>{rootNamespace}</RootNamespace>");
        lines.Add("  </PropertyGroup>");

        if (packages.Count > 0)
        {
            lines.Add("");
            lines.Add("  <ItemGroup>");

            foreach (var package in packages)
            {
                lines.Add(
                    $"    <PackageReference Include=\"{package}\" Version=\"{LaranetVersion.Current}\" />");
            }

            lines.Add("  </ItemGroup>");
        }

        lines.Add("");
        lines.Add("</Project>");

        return string.Join('\n', lines) + "\n";
    }

    /// <summary>
    /// Generates the entry point for a template.
    /// </summary>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <param name="template">The project template.</param>
    /// <returns>The program content.</returns>
    public static string Program(string rootNamespace, ProjectTemplate template)
    {
        return template switch
        {
            ProjectTemplate.WebApi => WebApiProgramTemplate,
            ProjectTemplate.Console => ConsoleProgramTemplate.Replace(
                "{{Namespace}}",
                rootNamespace,
                StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null),
        };
    }

    /// <summary>
    /// Gets the application settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string AppSettings()
    {
        return AppSettingsTemplate;
    }

    /// <summary>
    /// Gets the development application settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string AppSettingsDevelopment()
    {
        return AppSettingsDevelopmentTemplate;
    }

    /// <summary>
    /// Gets the launch settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string LaunchSettings()
    {
        return LaunchSettingsTemplate;
    }

    /// <summary>
    /// Gets the content of the generated .gitignore file.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string GitIgnore()
    {
        return GitIgnoreTemplate;
    }

    /// <summary>
    /// Generates the content of the project README.
    /// </summary>
    /// <param name="projectName">The project folder name.</param>
    /// <returns>The file content.</returns>
    public static string Readme(string projectName)
    {
        return ReadmeTemplate.Replace(
            "{{ProjectName}}",
            projectName,
            StringComparison.Ordinal);
    }
}
