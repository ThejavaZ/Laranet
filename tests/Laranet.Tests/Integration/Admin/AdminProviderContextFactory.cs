using Laranet.Admin;
using Laranet.Admin.EntityFrameworkCore;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Laranet.Tests.Integration.Admin;

internal static class AdminProviderContextFactory
{
    private const string DefaultSqlServerConnection =
        "Server=localhost,1433;Database=laranet_tests;User Id=sa;Password=Laranet!Tests1;TrustServerCertificate=True;Encrypt=True;Pooling=False";
    private const string DefaultPostgreSqlConnection =
        "Host=localhost;Port=5432;Database=laranet_tests;Username=postgres;Password=laranet;Pooling=false";
    private const string DefaultMySqlConnection =
        "Server=localhost;Port=3306;Database=laranet_tests;User Id=root;Password=laranet;SslMode=Disabled;AllowPublicKeyRetrieval=True;Pooling=False";

    public static async Task<AdminProviderDatabase> CreateAsync(
        DataTestProvider provider,
        Action<AdminProviderContext> seed)
    {
        var (context, connection) = await CreateContextAsync(provider);
        await context.Database.EnsureCreatedAsync();
        seed(context);
        await context.SaveChangesAsync();

        return new AdminProviderDatabase(context, provider, connection);
    }

    private static async Task<(AdminProviderContext Context, IDisposable? Connection)> CreateContextAsync(
        DataTestProvider provider)
    {
        switch (provider)
        {
            case DataTestProvider.Sqlite:
                {
                    var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
                    await connection.OpenAsync();
                    var options = new DbContextOptionsBuilder<AdminProviderContext>()
                        .UseSqlite(connection)
                        .Options;
                    return (new AdminProviderContext(options), connection);
                }

            case DataTestProvider.SqlServer:
                {
                    var connectionString = Environment.GetEnvironmentVariable("LARANET_SQLSERVER_CONNECTION")
                        ?? DefaultSqlServerConnection;
                    var builder = new SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = $"LaranetAdminTests_{Guid.NewGuid():N}",
                        Pooling = false,
                    };
                    var options = new DbContextOptionsBuilder<AdminProviderContext>()
                        .UseSqlServer(builder.ConnectionString)
                        .Options;
                    var context = new AdminProviderContext(options);
                    return (context, null);
                }

            case DataTestProvider.PostgreSql:
                {
                    var connectionString = Environment.GetEnvironmentVariable("LARANET_POSTGRES_CONNECTION")
                        ?? DefaultPostgreSqlConnection;
                    var builder = new NpgsqlConnectionStringBuilder(connectionString)
                    {
                        Database = $"laranet_admin_tests_{Guid.NewGuid():N}",
                        Pooling = false,
                    };
                    var options = new DbContextOptionsBuilder<AdminProviderContext>()
                        .UseNpgsql(builder.ConnectionString)
                        .Options;
                    var context = new AdminProviderContext(options);
                    return (context, null);
                }

            case DataTestProvider.MySql:
                {
                    var connectionString = Environment.GetEnvironmentVariable("LARANET_MYSQL_CONNECTION")
                        ?? DefaultMySqlConnection;
                    var builder = new MySqlConnectionStringBuilder(connectionString)
                    {
                        Database = $"laranet_admin_tests_{Guid.NewGuid():N}",
                    };
                    var options = new DbContextOptionsBuilder<AdminProviderContext>()
                        .UseMySQL(builder.ConnectionString)
                        .Options;
                    var context = new AdminProviderContext(options);
                    return (context, null);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }
    }

    public sealed class AdminProviderContext(DbContextOptions<AdminProviderContext> options)
        : DbContext(options)
    {
        public DbSet<AdminProviderProduct> Products => Set<AdminProviderProduct>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdminProviderProduct>(entity =>
            {
                entity.Property(product => product.Id).ValueGeneratedOnAdd();
            });
        }
    }

    public sealed class AdminProviderProduct
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }
}

internal sealed class AdminProviderDatabase : IAsyncDisposable
{
    private readonly IDisposable? _connection;

    public AdminProviderDatabase(
        AdminProviderContextFactory.AdminProviderContext context,
        DataTestProvider provider,
        IDisposable? connection = null)
    {
        Context = context;
        Provider = provider;
        _connection = connection;
    }

    public AdminProviderContextFactory.AdminProviderContext Context { get; }

    public DataTestProvider Provider { get; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await Context.DisposeAsync();
            _connection?.Dispose();
        }
    }
}
