using Laranet.Admin;
using Laranet.Admin.EntityFrameworkCore;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Admin;

internal static class AdminProviderContextFactory
{
    public static async Task<AdminProviderDatabase> CreateAsync(
        DataTestProvider provider,
        Action<AdminProviderContext> seed)
    {
        var context = CreateContext(provider, out var connection);
        await context.Database.EnsureCreatedAsync();
        seed(context);
        await context.SaveChangesAsync();

        return new AdminProviderDatabase(context, provider, connection);
    }

    private static AdminProviderContext CreateContext(
        DataTestProvider provider,
        out SqliteConnection? connection)
    {
        connection = null;
        var builder = new DbContextOptionsBuilder<AdminProviderContext>();

        switch (provider)
        {
            case DataTestProvider.Sqlite:
                connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                builder.UseSqlite(connection);
                break;
            case DataTestProvider.SqlServer:
                builder.UseSqlServer(
                    Environment.GetEnvironmentVariable("LARANET_SQLSERVER_CONNECTION")
                    ?? "Server=localhost,1433;Database=laranet_tests;User Id=sa;Password=Laranet!Tests1;TrustServerCertificate=True;Encrypt=True;Pooling=False");
                break;
            case DataTestProvider.PostgreSql:
                builder.UseNpgsql(
                    Environment.GetEnvironmentVariable("LARANET_POSTGRES_CONNECTION")
                    ?? "Host=localhost;Port=5432;Database=laranet_tests;Username=postgres;Password=laranet;Pooling=false");
                break;
            case DataTestProvider.MySql:
                builder.UseMySQL(
                    Environment.GetEnvironmentVariable("LARANET_MYSQL_CONNECTION")
                    ?? "Server=localhost;Port=3306;Database=laranet_tests;User Id=root;Password=laranet;SslMode=Disabled;AllowPublicKeyRetrieval=True;Pooling=False");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }

        return new AdminProviderContext(builder.Options);
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
    private readonly SqliteConnection? _connection;

    public AdminProviderDatabase(
        AdminProviderContextFactory.AdminProviderContext context,
        DataTestProvider provider,
        SqliteConnection? connection = null)
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
