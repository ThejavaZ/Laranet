using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes <c>admin:install</c> from parsed command-line input.
/// </summary>
internal static class AdminInstallRunner
{
    public static int Run(
        ParseResult parseResult,
        Option<string> outputOption,
        Option<string?> namespaceOption,
        Option<bool> forceOption,
        Option<bool> dryRunOption,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var outputDirectory = Path.GetFullPath(
                parseResult.GetValue(outputOption) ?? ".");
            var baseNamespace = NamespaceResolver.Resolve(
                outputDirectory,
                parseResult.GetValue(namespaceOption));

            // Validate admin package
            AdminPackageValidator.ValidateAdminPackage(outputDirectory);

            var files = new List<GeneratedFile>();

            files.Add(new GeneratedFile(
                Path.Combine(outputDirectory, "Admin", "README.md"),
                """
                # Admin Installation

                ## Next steps

                1. Add `builder.Services.AddRazorPages();` to Program.cs
                2. Add `builder.Services.AddLaranetAdmin();` to Program.cs
                3. Add `builder.Services.AddScoped<IAdminDataAdapter, EfCoreAdminDataAdapter>();`
                4. Add `app.MapRazorPages();` to Program.cs
                5. Create your first resource: `laranet admin:resource <Entity>`
                6. Run the app and visit /admin/<ResourceName>

                 ## Authentication

                 For a standalone development login, call
                 `builder.Services.AddLaranetAdminAuthentication(...)` and configure
                 an `IAdminUserStore`. You can also use your existing ASP.NET Core
                 authentication scheme. Do not use the in-memory store in production.
                """));

            var writer = new GenerationWriter(output, error, outputDirectory);
            var plan = new GenerationPlan("Admin", files);

            var result = writer.Write(
                plan,
                new GenerationOptions(
                    parseResult.GetValue(forceOption),
                    parseResult.GetValue(dryRunOption)));

            if (result == 0)
            {
                CliStyle.WriteSuccess(output, "Admin panel installed.");
                CliStyle.WriteNextStep(output, "Run `laranet admin:resource <Entity>` to create your first resource.");
            }

            return result;
        }
        catch (CliException exception)
        {
            error.WriteLine($"error: {exception.Message}");
            return 1;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }
}
