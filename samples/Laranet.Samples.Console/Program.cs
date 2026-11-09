using Laranet.Data.Extensions;
using Laranet.Data.Sorting;

// Laranet.Data works with any IQueryable<T>. This sample uses LINQ to Objects,
// so no database is required to see the query composition in action.
var products = new List<Product>
{
    new(1, "Mechanical keyboard", 120.00m, true),
    new(2, "Wireless mouse", 45.50m, true),
    new(3, "USB-C hub", 79.99m, true),
    new(4, "Laptop stand", 35.00m, false),
    new(5, "4K monitor", 349.00m, true),
    new(6, "Noise-cancelling headphones", 199.00m, true),
    new(7, "Webcam", 89.90m, true),
};

IQueryable<Product> query = products.AsQueryable();

// Optional values usually come from a request or configuration.
string? search = "o";
decimal? minPrice = 50m;
bool onlyActive = true;

var sortableColumns = new[] { nameof(Product.Name), nameof(Product.Price) };
var sortBy = nameof(Product.Price);
var direction = SortDirection.Descending;

var page = query
    .Search(product => product.Name, search)
    .WhereIf(minPrice.HasValue, product => product.Price >= minPrice!.Value)
    .WhereIf(onlyActive, product => product.IsActive)
    .OrderBy(sortBy, sortableColumns, direction)
    .Paginate(page: 1, pageSize: 3);

Console.WriteLine($"Search: '{search}' | Minimum price: {minPrice} | Active only: {onlyActive}");
Console.WriteLine($"Sorted by {sortBy} ({direction})");
Console.WriteLine($"Page {page.Page} of {page.TotalPages} ({page.TotalCount} matching products)");
Console.WriteLine();

foreach (var product in page.Items)
{
    Console.WriteLine($"  {product.Name,-30} {product.Price,8:0.00}  Active: {product.IsActive}");
}

Console.WriteLine();
Console.WriteLine($"Has previous page: {page.HasPreviousPage}");
Console.WriteLine($"Has next page:     {page.HasNextPage}");

internal sealed record Product(int Id, string Name, decimal Price, bool IsActive);
