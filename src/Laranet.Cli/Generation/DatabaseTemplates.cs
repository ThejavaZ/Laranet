namespace Laranet.Cli.Generation;

internal static class DatabaseTemplates
{
    public static string Context(
        GenerationContext context,
        string @namespace,
        string contextName,
        IReadOnlyList<string> entities)
    {
        var sets = entities.Count == 0
            ? string.Empty
            : string.Join(
                Environment.NewLine,
                entities.Select(entity =>
                    $"    public DbSet<{entity}> {CrudNames.PluralizeType(entity)} => Set<{entity}>();"));

        var entityUsing = entities.Count == 0
            ? string.Empty
            : $"using {context.BaseNamespace}.Models;\n\n";

        return $$"""
            using Microsoft.EntityFrameworkCore;
            {{entityUsing}}namespace {{@namespace}};

            public sealed class {{contextName}} : DbContext
            {
                public {{contextName}}(DbContextOptions<{{contextName}}> options)
                    : base(options)
                {
                }

            {{sets}}
                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    base.OnModelCreating(modelBuilder);
                    modelBuilder.ApplyConfigurationsFromAssembly(typeof({{contextName}}).Assembly);
                }
            }

            """;
    }

    public static string Configuration(
        GenerationContext context,
        string @namespace,
        string entity)
    {
        return $$"""
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Metadata.Builders;
            using {{context.BaseNamespace}}.Models;

            namespace {{@namespace}};

            public sealed class {{entity}}Configuration
                : IEntityTypeConfiguration<{{entity}}>
            {
                public void Configure(EntityTypeBuilder<{{entity}}> builder)
                {
                    builder.ToTable("{{CrudNames.PluralizeType(entity)}}");
                }
            }

            """;
    }

    public static string Factory(
        GenerationContext context,
        string @namespace,
        string contextNamespace,
        string contextName,
        DatabaseProvider provider)
    {
        var providerUsing = ProviderUsing(provider);
        var configure = string.Join(
            "\n",
            ProviderConfiguration(provider, contextName)
                .Split('\n')
                .Select(line => string.IsNullOrWhiteSpace(line) ? line : "        " + line));
        var providerUsingLine = string.IsNullOrWhiteSpace(providerUsing)
            ? string.Empty
            : providerUsing + "\n";

        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using {contextNamespace};

{providerUsingLine}namespace {@namespace};

public sealed class {contextName}Factory
    : IDesignTimeDbContextFactory<{contextName}>
{{
    public {contextName} CreateDbContext(string[] args)
    {{
        var optionsBuilder = new DbContextOptionsBuilder<{contextName}>();
{configure}

        return new {contextName}(optionsBuilder.Options);
    }}
}}
";

    }

    private static string ProviderUsing(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => string.Empty,
            DatabaseProvider.Postgres => "using Npgsql.EntityFrameworkCore.PostgreSQL;",
            DatabaseProvider.MySql => "using Pomelo.EntityFrameworkCore.MySql.Infrastructure;",
            DatabaseProvider.Sqlite => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };
    }

    private static string ProviderConfiguration(DatabaseProvider provider, string contextName)
    {
        var method = provider switch
        {
            DatabaseProvider.SqlServer => "UseSqlServer",
            DatabaseProvider.Postgres => "UseNpgsql",
            DatabaseProvider.MySql => "UseMySql",
            DatabaseProvider.Sqlite => "UseSqlite",
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };

        var mySqlSuffix = provider == DatabaseProvider.MySql
            ? ", ServerVersion.AutoDetect(connectionString)"
            : string.Empty;

        return $$"""
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__{{contextName}}")
            ?? throw new InvalidOperationException(
                "Set the ConnectionStrings__{{contextName}} environment variable before running design-time commands.");

        optionsBuilder.{{method}}(connectionString{{mySqlSuffix}});
        """;
    }
}
