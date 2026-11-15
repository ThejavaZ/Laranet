# Admin prototype

> **Status:** Experimental prototype. The contracts are not a stable API and the UI
> package is not published.

Sprint 20 validates a first administrative use case with:

- `Product` as the managed resource.
- SQLite and EF Core as the data provider.
- Razor Pages as the UI.
- ASP.NET Core Identity and an explicit `Admin.Products` policy.

Run the sample:

```bash
dotnet run --project samples/Laranet.Samples.Admin
```

The sample demonstrates the intended boundary:

```text
Laranet.Admin
    ├── resource metadata
    ├── query and page contracts
    ├── CRUD data adapter contract
    └── authorization hook

Application
    ├── EF Core SQLite adapter
    ├── Razor Pages
    └── ASP.NET Core Identity
```

`Laranet.Admin` does not reference EF Core, ASP.NET Core, Razor, Identity, or a
database provider. The application owns mapping, query composition, persistence,
authentication, authorization, and page rendering.

The experimental `Laranet.Admin.AspNetCore` package can provide the conventional
Razor Pages surface:

```csharp
builder.Services.AddLaranetAdmin();
builder.Services.AddRazorPages();

app.MapRazorPages();
```

It exposes `/admin/{resource}` plus conventional create and edit routes. The
application still provides `IAdminDataAdapter`, resources, authentication, and
authorization policies. The package does not include EF Core or Identity.

## Resource registration

Register resources explicitly:

```csharp
var registry = new AdminResourceRegistry();
registry.Add(ProductAdminResource.Definition);
```

A resource defines fields, filters, actions, and a permission policy name:

```csharp
public static AdminResource Definition { get; } = new(
    "Products",
    typeof(Product),
    [
        new AdminField("Id", "ID", typeof(int), IsKey: true, IsReadOnly: true),
        new AdminField("Name", "Name", typeof(string)),
        new AdminField("Price", "Price", typeof(decimal)),
    ],
    [new AdminFilter("Name", "Name", typeof(string))],
    [
        new AdminAction("Create", "Create product", MutatesData: true),
        new AdminAction("Edit", "Edit product", MutatesData: true),
        new AdminAction("Delete", "Delete product", MutatesData: true),
    ],
    "Admin.Products");
```

For typed resource configuration, the experimental builder provides conventions for
search, filtering, ordering, forms, formatting, and permissions:

```csharp
public sealed class ProductResource : AdminResource<Product>
{
    protected override void Configure(AdminResourceBuilder<Product> resource)
    {
        resource.Field(product => product.Name)
            .Searchable()
            .Orderable();
        resource.Field(product => product.Price)
            .FormatAs("currency");
        resource.Filter(product => product.IsActive);
        resource.Action("Delete", "Delete product", mutatesData: true);
        resource.Permission("Admin.Products");
    }
}
```

The builder produces provider-neutral metadata. It does not execute queries or render
pages, and its exact API remains experimental until a real application validates it.

## Adapter boundary

Implement `IAdminDataAdapter` in the application or an integration package. The
adapter receives `AdminResource` and `AdminQuery`, then owns provider-specific
filtering, ordering, pagination, mapping, and CRUD:

```csharp
public interface IAdminDataAdapter
{
    Task<AdminPage> ListAsync(
        AdminResource resource,
        AdminQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, object?>?> ReadAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default);

    // CreateAsync, UpdateAsync, and DeleteAsync are also required.
}
```

The core contract deliberately does not expose `IQueryable`, `DbContext`, or
provider-specific types.

## Security boundary

Use native ASP.NET Core authentication and authorization. The prototype uses Identity
and protects every Products page with the `Admin.Products` policy:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin.Products", policy => policy.RequireAuthenticatedUser());
```

`IAdminAuthorization` is only a permission hook. It does not manage users, passwords,
roles, tokens, cookies, or antiforgery. Razor Pages supplies native antiforgery
protection for mutating forms.

## Current limits

- Only the Product sample is validated.
- Resource discovery is explicit.
- There is no automatic CRUD UI generator.
- There is no custom view engine or JavaScript component library.
- `Laranet.Admin.AspNetCore` is not published.
- A real application must validate the contracts before the API is stabilized.

See [ADR-0012](../architecture/decisions/0012-admin-boundary-and-prototype.md) for
the boundary and [Version 3 Sprint 20](../roadmap/v3.md) for the admission status.
