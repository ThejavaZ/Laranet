
using Laranet.Data.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class SearchIntegrationTests
{
    [Fact]
    public async Task Search_ShouldBeTranslatedAndExecutedBySqlite()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SearchTestDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SearchTestDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.Products.AddRange(
            new SearchTestProduct { Name = "Mechanical Keyboard" },
            new SearchTestProduct { Name = "Gaming Mouse" },
            new SearchTestProduct { Name = "Wireless Keyboard" },
            new SearchTestProduct { Name = null }
        );

        await context.SaveChangesAsync();

        // Act
        var query = context.Products.Search(
            product => product.Name,
            "Keyboard"
        );

        // Assert: EF Core must translate the expression to SQL.
        var sql = query.ToQueryString();

        Assert.Contains("instr", sql, StringComparison.OrdinalIgnoreCase);

        // Assert: SQLite must execute the query successfully.
        var results = await query
            .OrderBy(product => product.Id)
            .ToListAsync();

        Assert.Equal(2, results.Count);

        Assert.All(results, product =>
            Assert.Contains("Keyboard", product.Name!)
        );
    }

    [Fact]
    public async Task Search_ShouldParameterizeTerm_WhenExecutedBySqlite()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var interceptor = new CommandCaptureInterceptor();

        var options = new DbContextOptionsBuilder<SearchTestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new SearchTestDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.Products.AddRange(
            new SearchTestProduct { Name = "Mechanical Keyboard" },
            new SearchTestProduct { Name = "Gaming Mouse" },
            new SearchTestProduct { Name = "Wireless Keyboard" },
            new SearchTestProduct { Name = null }
        );

        await context.SaveChangesAsync();

        interceptor.Clear();

        // Act
        var results = await context.Products
            .Search(product => product.Name, "Keyboard")
            .OrderBy(product => product.Id)
            .ToListAsync();

        // Assert: behavior is preserved.
        Assert.Equal(2, results.Count);

        Assert.All(results, product =>
            Assert.Contains("Keyboard", product.Name!)
        );

        // Assert: the term is sent as a parameter, not as a SQL literal.
        var command = Assert.Single(interceptor.Commands);

        Assert.Contains(
            "instr",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.DoesNotContain(
            "'Keyboard'",
            command.CommandText,
            StringComparison.Ordinal
        );

        Assert.Contains(
            command.Parameters,
            parameter => Equals(parameter.Value, "Keyboard")
        );
    }
}

internal sealed class SearchTestDbContext(
    DbContextOptions<SearchTestDbContext> options
) : DbContext(options)
{
    public DbSet<SearchTestProduct> Products => Set<SearchTestProduct>();
}

internal sealed class SearchTestProduct
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
