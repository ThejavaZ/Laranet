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
        var configure = ProviderConfiguration(provider);

        return $$"""
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Design;
            using {{contextNamespace}};
            {{providerUsing}}
            namespace {{@namespace}};

            public sealed class {{contextName}}Factory
                : IDesignTimeDbContextFactory<{{contextName}}>
            {
                public {{contextName}} CreateDbContext(string[] args)
                {
                    var optionsBuilder = new DbContextOptionsBuilder<{{contextName}}>();
                    {{configure}}

                    return new {{contextName}}(optionsBuilder.Options);
                }
            }

            """;
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

    private static string ProviderConfiguration(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer =>
                "optionsBuilder.UseSqlServer(\"Server=(localdb)\\\\mssqllocaldb;Database=AppDb;Trusted_Connection=True;\");",
            DatabaseProvider.Postgres =>
                "optionsBuilder.UseNpgsql(\"Host=localhost;Database=app;Username=postgres;Password=postgres\");",
            DatabaseProvider.MySql =>
                "optionsBuilder.UseMySql(\"Server=localhost;Database=app;User=root;Password=\", ServerVersion.AutoDetect(\"Server=localhost;Database=app;User=root;Password=\"));",
            DatabaseProvider.Sqlite =>
                "optionsBuilder.UseSqlite(\"Data Source=app.db\");",
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };
    }
}
