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

## The panel

`Laranet.Admin.AspNetCore` ships conventional Razor Pages under `/admin`:

| Route | Page |
| --- | --- |
| `/admin` | Dashboard listing the resources the user is allowed to list |
| `/admin/{resource}` | Resource list with search, filters, and pagination |
| `/admin/{resource}/create` | Create form |
| `/admin/{resource}/{key}` | Record details |
| `/admin/{resource}/{key}/edit` | Edit form |
| `/admin/login` | Login, protected by an in-memory rate limiter |
| `/admin/logout` | Sign out |
| `/admin/password`, `/admin/forgot-password`, `/admin/reset-password` | Password management |
| `/admin/mfa`, `/admin/mfa/setup` | Two-factor authentication |

Every page challenges unauthenticated requests and evaluates the resource's
permission policy through `IAdminAuthorization` before rendering data. The
panel does not ship its own identity store: it authenticates through the
services registered by `AddLaranetAdminAuthentication(...)`, where users are
provided by configuration (`options.AddUser(...)`) or by an application-provided
`IAdminUserStore`. The complete wiring is validated in
[`Laranet.Samples.Admin`](../samples/Laranet.Samples.Admin).

## Auto-discovery

Resources can be auto-discovered from an assembly using the `AdminResourceAttribute`:

```csharp
[AdminResource(Name = "Products", PermissionPolicy = "Admin.Products")]
public sealed class ProductAdminResource : AdminResource<Product>
{
    protected override void Configure(AdminResourceBuilder<Product> resource)
    {
        resource.Field(product => product.Name).Searchable().Orderable();
        resource.Field(product => product.Price).FormatAs("currency");
        resource.Field(product => product.IsActive).Filterable();
        resource.Permission("Admin.Products");
    }
}
```

Register all discovered resources:

```csharp
builder.Services.AddSingleton<IAdminResourceRegistry>(services =>
{
    var registry = new AdminResourceRegistry();
    AdminResourceDiscovery.RegisterAll(registry, typeof(Program).Assembly);
    return registry;
});
```

Auto-discovery is optional. Explicit registration remains the recommended approach
for most applications.

## Integration with ASP.NET Core Identity

The admin panel integrates with native ASP.NET Core Identity:

```csharp
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AdminDbContext>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin.Products", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
```

The `IAdminAuthorization` implementation bridges to the native authorization
service:

```csharp
public sealed class IdentityAdminAuthorization : IAdminAuthorization
{
    private readonly IAuthorizationService _authorization;

    public IdentityAdminAuthorization(IAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    public async Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        AdminResource resource,
        AdminOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource.PermissionPolicy))
        {
            return principal.Identity?.IsAuthenticated == true;
        }

        var result = await _authorization.AuthorizeAsync(
            principal, resource.PermissionPolicy);

        return result.Succeeded;
    }
}
```

## Complete examples

### Product resource with all features

```csharp
public sealed class ProductAdminResource : AdminResource<Product>
{
    protected override void Configure(AdminResourceBuilder<Product> resource)
    {
        resource.Field(product => product.Id, "ID")
            .Orderable();

        resource.Field(product => product.Name)
            .Searchable()
            .Orderable()
            .Required()
            .MaxLength(100);

        resource.Field(product => product.Description)
            .AsTextArea(rows: 4);

        resource.Field(product => product.Price)
            .FormatAs("currency")
            .Min("0");

        resource.Field(product => product.IsActive)
            .AsCheckbox()
            .Filterable();

        resource.Field(product => product.CategoryId)
            .BelongsTo<Category, Guid>("Category", "Id", "Name");

        resource.Field(product => product.CreatedAt)
            .AsDate()
            .ReadOnly();

        resource.Field(product => product.UpdatedAt)
            .AsDateTime()
            .ReadOnly();

        resource.Filter(product => product.IsActive, "Active");
        resource.Filter(product => product.CategoryId, "Category");

        resource.Action("Activate", "Activate", MutatesData: true);
        resource.Action("Deactivate", "Deactivate", MutatesData: true);
        resource.Action("Delete", "Delete", MutatesData: true);

        resource.Permission("Admin.Products");
        resource.OrderBy(product => product.Name);
    }
}
```

### Category resource with delete validation

```csharp
public sealed class CategoryAdminResource : AdminResource<Category>
{
    protected override void Configure(AdminResourceBuilder<Category> resource)
    {
        resource.Field(category => category.Name).Searchable().Required();
        resource.Field(category => category.Description).AsTextArea();

        resource.Permission("Admin.Categories");

        resource.DeleteValidator(async (entity, cancellationToken) =>
        {
            // Check if any products reference this category
            // Return an error message if deletion should be blocked
            return null; // Allow deletion
        });
    }
}
```

### Multi-tenant resource

```csharp
public sealed class ProductAdminResource : AdminResource<Product>
{
    protected override void Configure(AdminResourceBuilder<Product> resource)
    {
        resource.Field(product => product.CompanyId).AsHidden();
        resource.Field(product => product.Name).Searchable().Required();

        resource.Permission("Admin.Products");
    }
}
```

## Current limits

- Only the Product sample is validated.
- Resource discovery is explicit.
- There is no automatic CRUD UI generator.
- There is no custom view engine or JavaScript component library.
- `Laranet.Admin.AspNetCore` is not published.
- A real application must validate the contracts before the API is stabilized.

See [ADR-0012](../architecture/decisions/0012-admin-boundary-and-prototype.md) for
the boundary and [Version 3 Sprint 20](../roadmap/v3.md) for the admission status.
