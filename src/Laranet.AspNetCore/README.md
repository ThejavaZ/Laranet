# Laranet.AspNetCore

ASP.NET Core integration for `Laranet.Data` pagination responses.

This package exposes pagination metadata through standard HTTP headers and provides
a ready-to-use result for Minimal API endpoints. It does not define a new response
envelope: the JSON body remains the native `PagedResult<T>` shape.

## Installation

```bash
dotnet add package Laranet.AspNetCore
```

## Usage

### Minimal APIs

```csharp
using Laranet.AspNetCore.Extensions;
using Laranet.EntityFrameworkCore.Extensions;

app.MapGet("/products", async (
    int page,
    int pageSize,
    AppDbContext db,
    CancellationToken cancellationToken) =>
{
    var result = await db.Products
        .OrderBy(product => product.Id)
        .PaginateAsync(
            page,
            pageSize,
            cancellationToken: cancellationToken);

    return result.ToHttpResult();
});
```

`ToHttpResult` writes the paginated result as JSON and adds the pagination headers to
the response.

### MVC controllers

```csharp
using Laranet.AspNetCore.Extensions;
using Laranet.EntityFrameworkCore.Extensions;

[HttpGet]
public async Task<IActionResult> Get(
    int page = 1,
    int pageSize = 15,
    CancellationToken cancellationToken = default)
{
    var result = await _db.Products
        .OrderBy(product => product.Id)
        .PaginateAsync(
            page,
            pageSize,
            cancellationToken: cancellationToken);

    result.WritePaginationHeaders(Response);

    return Ok(result);
}
```

`WritePaginationHeaders` can be used in any ASP.NET Core code path where an
`HttpResponse` is available.

## Pagination headers

| Header          | Meaning                              |
| --------------- | ------------------------------------ |
| `X-Total-Count` | Total number of items.               |
| `X-Page`        | Current page number.                 |
| `X-Page-Size`   | Effective page size after clamping.  |
| `X-Total-Pages` | Total number of available pages.     |

The JSON body keeps the `PagedResult<T>` properties: `items`, `page`, `pageSize`,
`totalCount`, `totalPages`, `hasPreviousPage`, and `hasNextPage`.

## Requirements

- .NET 10
- ASP.NET Core 10.x

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
