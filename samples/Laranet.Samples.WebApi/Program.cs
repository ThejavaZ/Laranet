using Laranet.AspNetCore.Extensions;
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
using Laranet.EntityFrameworkCore.Extensions;
using Laranet.Samples.WebApi.Contracts;
using Laranet.Samples.WebApi.Data;
using Laranet.Samples.WebApi.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// A single open SQLite connection keeps the in-memory database alive for the
// lifetime of the application.
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

builder.Services.AddSingleton(connection);

builder.Services.AddDbContext<CatalogDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<SqliteConnection>()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    await db.Database.EnsureCreatedAsync();
    await SeedAsync(db);
}

var sortableColumns = new[]
{
    nameof(Product.Name),
    nameof(Product.Price),
    nameof(Product.CreatedAt),
};

app.MapGet("/", () => Results.Redirect("/products"));

app.MapGet("/products", async (
    CatalogDbContext db,
    CancellationToken cancellationToken,
    int page = 1,
    int pageSize = 10,
    string? search = null,
    decimal? minPrice = null,
    bool? onlyActive = null,
    string? sortBy = null,
    SortDirection direction = SortDirection.Ascending) =>
{
    var sortProperty = sortBy ?? nameof(Product.Name);

    if (!sortableColumns.Contains(sortProperty, StringComparer.Ordinal))
    {
        return Results.BadRequest(new
        {
            error = $"Unknown sort property '{sortProperty}'.",
            allowed = sortableColumns,
        });
    }

    var minPriceValue = minPrice.GetValueOrDefault();

    var result = await db.Products
        .Search(product => product.Name, search)
        .WhereIf(minPrice.HasValue, product => product.Price >= minPriceValue)
        .WhereIf(onlyActive == true, product => product.IsActive)
        .OrderBy(sortProperty, sortableColumns, direction)
        .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);

    return result.ToHttpResult();
});

app.MapGet("/products/{id:int}", async (
    int id,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var product = await db.Products.FindAsync([id], cancellationToken);

    return product is null
        ? Results.NotFound()
        : Results.Ok(product);
});

app.MapPost("/products", async (
    CreateProductRequest request,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = new[] { "Name is required." },
        });
    }

    if (request.Price < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["price"] = new[] { "Price cannot be negative." },
        });
    }

    var product = new Product
    {
        Name = request.Name.Trim(),
        Price = request.Price,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    db.Products.Add(product);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/products/{product.Id}", product);
});

app.Run();

static async Task SeedAsync(CatalogDbContext db)
{
    if (await db.Products.AnyAsync())
    {
        return;
    }

    var createdAt = DateTime.UtcNow;

    db.Products.AddRange(
        new Product
        {
            Name = "Mechanical keyboard",
            Price = 120.00m,
            IsActive = true,
            CreatedAt = createdAt,
        },
        new Product
        {
            Name = "Wireless mouse",
            Price = 45.50m,
            IsActive = true,
            CreatedAt = createdAt,
        },
        new Product
        {
            Name = "4K monitor",
            Price = 349.00m,
            IsActive = true,
            CreatedAt = createdAt,
        },
        new Product
        {
            Name = "Laptop stand",
            Price = 35.00m,
            IsActive = false,
            CreatedAt = createdAt,
        });

    await db.SaveChangesAsync();
}
