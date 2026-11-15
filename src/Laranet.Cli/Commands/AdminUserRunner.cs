using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes <c>admin:user</c> from parsed command-line input.
/// </summary>
internal static class AdminUserRunner
{
    public static int Run(
        ParseResult parseResult,
        AdminUserArguments arguments,
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var interaction = parseResult.GetValue(arguments.Interactive)
                ? new ConsoleInteraction(input, output)
                : null;

            var name = ResolveName(parseResult, arguments, interaction);
            var outputDirectory = Path.GetFullPath(
                parseResult.GetValue(arguments.Output) ?? ".");

            var baseNamespace = ResolveNamespace(
                parseResult,
                arguments,
                interaction,
                outputDirectory);

            // Validate admin package
            AdminPackageValidator.ValidateAdminPackage(outputDirectory);

            var seederName = NameNormalizer.EnsureSuffix(name, "Seeder");
            var target = GeneratorSupport.ResolveTarget(
                context: new GenerationContext(
                    name,
                    baseNamespace,
                    outputDirectory,
                    parseResult.GetValue(arguments.Flat),
                    TemplateCatalog.Discover(outputDirectory)),
                "Seeders");

            var content = $$"""
                using Microsoft.AspNetCore.Identity;
                using Microsoft.Extensions.Configuration;

                namespace {{target.Namespace}};

                /// <summary>
                /// Seeds the first administrator using credentials from
                /// configuration or environment variables.
                /// </summary>
                public static class {{seederName}}
                {
                    public static async Task SeedAsync(
                        IServiceProvider services,
                        CancellationToken cancellationToken = default)
                    {
                        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                        var configuration = services.GetRequiredService<IConfiguration>();

                        var email = configuration["Admin:Email"]
                            ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                            ?? throw new InvalidOperationException(
                                "Set Admin:Email in configuration or the ADMIN_EMAIL environment variable.");

                        var password = configuration["Admin:Password"]
                            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                            ?? throw new InvalidOperationException(
                                "Set Admin:Password in configuration or the ADMIN_PASSWORD environment variable.");

                        var existing = await userManager.FindByEmailAsync(email);

                        if (existing is not null)
                        {
                            return;
                        }

                        var user = new IdentityUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                        };

                        var result = await userManager.CreateAsync(user, password);

                        if (!result.Succeeded)
                        {
                            throw new InvalidOperationException(
                                string.Join("; ", result.Errors.Select(error => error.Description)));
                        }

                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                }
                """;

            var plan = new GenerationPlan(
                name,
                [new GeneratedFile(Path.Combine(target.Directory, $"{seederName}.cs"), content)]);

            var result = new GenerationWriter(output, error, outputDirectory, interaction).Write(
                plan,
                new GenerationOptions(
                    parseResult.GetValue(arguments.Force),
                    parseResult.GetValue(arguments.DryRun)));

            if (result == 0)
            {
                CliStyle.WriteSuccess(output, "Admin user seeder generated.");
                CliStyle.WriteNextStep(output, "Run the app with `--seed-admin` or call the seeder from startup.");
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

    private static string ResolveName(
        ParseResult parseResult,
        AdminUserArguments arguments,
        ConsoleInteraction? interaction)
    {
        var rawName = parseResult.GetValue(arguments.Name);

        return interaction is not null && string.IsNullOrWhiteSpace(rawName)
            ? interaction.AskUntil("Name: ", NameNormalizer.Normalize)
            : NameNormalizer.Normalize(rawName ?? "AdminUser");
    }

    private static string ResolveNamespace(
        ParseResult parseResult,
        AdminUserArguments arguments,
        ConsoleInteraction? interaction,
        string outputDirectory)
    {
        try
        {
            return NamespaceResolver.Resolve(
                outputDirectory,
                parseResult.GetValue(arguments.Namespace));
        }
        catch (CliException) when (interaction is not null)
        {
            return interaction.AskUntil("Namespace: ", value =>
            {
                NamespaceResolver.Validate(value);

                return value;
            });
        }
    }
}
