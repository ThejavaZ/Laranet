using Laranet.Data.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class SearchIntegrationTests
{
    private static void SeedProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            new DataTestProduct { Name = "Mechanical Keyboard" },
            new DataTestProduct { Name = "Gaming Mouse" },
            new DataTestProduct { Name = "Wireless Keyboard" },
            new DataTestProduct { Name = null }
        );
    }

    [Fact]
    public async Task Search_ShouldBeTranslatedAndExecutedBySqlite()
    {
        // Arrange
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        // Act
        var query = database.Context.Products.Search(
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
    public async Task Search_ShouldTranslateSeveralColumnsWithOr()
    {
        // Arrange
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: context => context.Products.AddRange(
                new DataTestProduct
                {
                    Name = "Mechanical Keyboard",
                    Description = "Wired",
                },
                new DataTestProduct
                {
                    Name = "Gaming Mouse",
                    Description = "Mechanical Keyboard switches",
                },
                new DataTestProduct
                {
                    Name = "Monitor",
                    Description = null,
                }));

        // Act
        var query = database.Context.Products.Search(
            [product => product.Name, product => product.Description],
            "Keyboard"
        );

        // Assert: both columns are translated into a single OR expression.
        var sql = query.ToQueryString();

        Assert.Contains("OR", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "Name",
            sql,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.Contains(
            "Description",
            sql,
            StringComparison.OrdinalIgnoreCase
        );

        // Assert: a match in any column includes the item.
        var results = await query
            .OrderBy(product => product.Id)
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Search_ShouldParameterizeTerm_WhenExecutedBySqlite()
    {
        // Arrange
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        // Act
        var results = await database.Context.Products
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
