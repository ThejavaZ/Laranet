# Laranet Samples

Small, runnable applications that show how the Laranet packages fit together.

| Sample | Packages | Description |
| --- | --- | --- |
| [`Laranet.Samples.Console`](Laranet.Samples.Console) | `Laranet.Data` | Composes search, conditional filters, sorting, and pagination over LINQ to Objects. |
| [`Laranet.Samples.WebApi`](Laranet.Samples.WebApi) | `Laranet.Data`, `Laranet.EntityFrameworkCore`, `Laranet.AspNetCore` | Minimal API with EF Core, SQLite, paginated queries, and pagination HTTP headers. |

The samples reference the projects in `src/` so they always exercise the current
source code. They are not packaged or published.

## Console sample

```bash
dotnet run --project samples/Laranet.Samples.Console
```

Expected output:

```text
Search: 'o' | Minimum price: 50 | Active only: True
Sorted by Price (Descending)
Page 1 of 1 (3 matching products)

  4K monitor                       349.00  Active: True
  Noise-cancelling headphones      199.00  Active: True
  Mechanical keyboard              120.00  Active: True

Has previous page: False
Has next page:     False
```

## Web API sample

```bash
dotnet run --project samples/Laranet.Samples.WebApi
```

Then query the catalog:

```bash
curl 'http://localhost:5000/products?page=1&pageSize=2&search=monitor'
```

The response body is a native `PagedResult<Product>` and the pagination metadata is
also exposed through headers:

```text
X-Total-Count: 4
X-Page: 1
X-Page-Size: 2
X-Total-Pages: 2
```

The sample uses an in-memory SQLite database seeded at startup. Data is reset every
time the application restarts.

### Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/products` | Paginated, searchable, sortable product list. |
| `GET` | `/products/{id}` | Single product. |
| `POST` | `/products` | Creates a product. |

### Query parameters

| Parameter | Default | Description |
| --- | --- | --- |
| `page` | `1` | Page number, starting at 1. |
| `pageSize` | `10` | Requested page size. |
| `search` | none | Searches the product name. |
| `minPrice` | none | Minimum price filter. |
| `onlyActive` | none | When `true`, returns active products only. |
| `sortBy` | `Name` | `Name`, `Price`, or `CreatedAt`. Unknown values return `400`. |
| `direction` | `Ascending` | `Ascending` or `Descending`. |

## Next steps

- [Getting started](../docs/getting-started/quick-start.md)
- [Data access guide](../docs/guides/data-access.md)
- [Pagination guide](../docs/guides/pagination.md)
