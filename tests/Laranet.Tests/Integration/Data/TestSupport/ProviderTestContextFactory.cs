using Laranet.EntityFrameworkCore.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Laranet.Tests.Integration.Data.TestSupport;

internal static class ProviderTestContextFactory
{
    private const string DefaultSqlServerConnection =
        "Server=localhost,1433;Database=laranet_tests;User Id=sa;Password=Laranet!Tests1;TrustServerCertificate=True;Encrypt=True;Pooling=False";

    private const string DefaultPostgreSqlConnection =
        "Host=localhost;Port=5432;Database=laranet_tests;Username=postgres;Password=laranet;Pooling=false";

    private const string DefaultMySqlConnection =
        "Server=localhost;Port=3306;Database=laranet_tests;User Id=root;Password=laranet;SslMode=Disabled;AllowPublicKeyRetrieval=True;Pooling=False";

    public static async Task<ProviderTestDatabase> CreateAsync(
        DataTestProvider provider,
        CommandCaptureInterceptor? interceptor = null,
        Action<DataTestDbContext>? seed = null,
        TimeProvider? timeProvider = null)
    {
        return provider switch
        {
            DataTestProvider.Sqlite => await CreateSqliteAsync(
                interceptor,
                seed,
                timeProvider),
            DataTestProvider.SqlServer => await CreateSqlServerAsync(
                interceptor,
                seed,
                timeProvider),
            DataTestProvider.PostgreSql => await CreatePostgreSqlAsync(
                interceptor,
                seed,
                timeProvider),
            DataTestProvider.MySql => await CreateMySqlAsync(
                interceptor,
                seed,
                timeProvider),
            _ => throw new ArgumentOutOfRangeException(
                nameof(provider),
                provider,
                null),
        };
    }

    private static async Task<ProviderTestDatabase> CreateSqliteAsync(
        CommandCaptureInterceptor? interceptor,
        Action<DataTestDbContext>? seed,
        TimeProvider? timeProvider)
    {
        var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            seed,
            timeProvider);

        return new ProviderTestDatabase(database.Context, database);
    }

    private static async Task<ProviderTestDatabase> CreateSqlServerAsync(
        CommandCaptureInterceptor? interceptor,
        Action<DataTestDbContext>? seed,
        TimeProvider? timeProvider)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ProviderTestData.SqlServerConnectionVariable)
            ?? DefaultSqlServerConnection;

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = $"LaranetTests_{Guid.NewGuid():N}",
            Pooling = false,
        };

        var optionsBuilder = new DbContextOptionsBuilder<DataTestDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .AddLaranetAuditing(timeProvider)
            .AddLaranetSoftDelete(timeProvider);

        return await CreateRelationalAsync(optionsBuilder, interceptor, seed);
    }

    private static async Task<ProviderTestDatabase> CreatePostgreSqlAsync(
        CommandCaptureInterceptor? interceptor,
        Action<DataTestDbContext>? seed,
        TimeProvider? timeProvider)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ProviderTestData.PostgreSqlConnectionVariable)
            ?? DefaultPostgreSqlConnection;

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = $"laranet_tests_{Guid.NewGuid():N}",
            Pooling = false,
        };

        var optionsBuilder = new DbContextOptionsBuilder<DataTestDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .AddLaranetAuditing(timeProvider)
            .AddLaranetSoftDelete(timeProvider);

        return await CreateRelationalAsync(optionsBuilder, interceptor, seed);
    }

    private static async Task<ProviderTestDatabase> CreateMySqlAsync(
        CommandCaptureInterceptor? interceptor,
        Action<DataTestDbContext>? seed,
        TimeProvider? timeProvider)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ProviderTestData.MySqlConnectionVariable)
            ?? DefaultMySqlConnection;

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Database = $"laranet_tests_{Guid.NewGuid():N}",
            Pooling = false,
        };

        var optionsBuilder = new DbContextOptionsBuilder<DataTestDbContext>()
            .UseMySQL(builder.ConnectionString)
            .AddLaranetAuditing(timeProvider)
            .AddLaranetSoftDelete(timeProvider);

        return await CreateRelationalAsync(optionsBuilder, interceptor, seed);
    }

    private static async Task<ProviderTestDatabase> CreateRelationalAsync(
        DbContextOptionsBuilder<DataTestDbContext> optionsBuilder,
        CommandCaptureInterceptor? interceptor,
        Action<DataTestDbContext>? seed)
    {
        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        var context = new DataTestDbContext(optionsBuilder.Options);

        try
        {
            await context.Database.EnsureCreatedAsync();

            if (seed is not null)
            {
                seed(context);
                await context.SaveChangesAsync();
            }

            interceptor?.Clear();

            return new ProviderTestDatabase(context);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }
}

internal sealed class ProviderTestDatabase : IAsyncDisposable
{
    private readonly TestDatabase? _sqliteDatabase;

    public ProviderTestDatabase(DataTestDbContext context)
    {
        Context = context;
    }

    public ProviderTestDatabase(
        DataTestDbContext context,
        TestDatabase sqliteDatabase)
    {
        Context = context;
        _sqliteDatabase = sqliteDatabase;
    }

    public DataTestDbContext Context { get; }

    public async ValueTask DisposeAsync()
    {
        if (_sqliteDatabase is not null)
        {
            await _sqliteDatabase.DisposeAsync();
            return;
        }

        try
        {
            await Context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Context.DisposeAsync();
        }
    }
}
