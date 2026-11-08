using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data.TestSupport;

internal static class SqliteTestContextFactory
{
    public static async Task<TestDatabase> CreateAsync(
        CommandCaptureInterceptor? interceptor = null,
        Action<DataTestDbContext>? seed = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var optionsBuilder = new DbContextOptionsBuilder<DataTestDbContext>()
            .UseSqlite(connection);

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

            return new TestDatabase(connection, context);
        }
        catch
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }
}

internal sealed class TestDatabase : IAsyncDisposable
{
    public TestDatabase(
        SqliteConnection connection,
        DataTestDbContext context)
    {
        Connection = connection;
        Context = context;
    }

    public SqliteConnection Connection { get; }

    public DataTestDbContext Context { get; }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
