# Laranet.Admin.AspNetCore

Experimental Razor Pages integration for `Laranet.Admin`.

The package provides conventional `/admin/{resource}` pages for listing, searching,
pagination, creating, editing, and deleting resource rows. Applications provide the
`IAdminDataAdapter`, register resources, and configure native ASP.NET Core
authentication and authorization.

The Razor UI is organized by responsibility:

- `Areas/Admin/Pages/Layouts` contains the application shell layouts.
- `Areas/Admin/Pages/Components` contains reusable partials such as the topbar,
  sidebar, toast, dialog, breadcrumbs, form fields and sections, button, table,
  pagination, and empty state.
- `Areas/Admin/Pages/Resources` contains the page models and page orchestration.

Form fields are grouped into native `<fieldset>` sections when `AdminField.Section` is
set, either through the record constructor or `Field(...).Section("Name")` in the builder.

Resources can provide an explicit singular label and dashboard icon:

```csharp
new AdminResource(/* ... */, Singular: "Product", Icon: "box")
```

Typed resources can configure the same values by overriding `Singular` and `Icon`.
Supported dashboard icons include `box`, `users`, `cart`, `file`, `tag`, `calendar`,
`chart`, `settings`, `folder`, `key`, `shield`, and `globe`.

The built-in admin UI uses English consistently for its labels, actions, and messages.

It does not include EF Core, Identity, a database provider, a custom view engine, or a
JavaScript framework. The API is experimental and is not part of the current release
train.

### Standalone authentication

The package includes an optional cookie-based login that does not depend on ASP.NET Core
Identity:

```csharp
builder.Services.AddLaranetAdminAuthentication(options =>
{
    options.AddUser(
        builder.Configuration["Admin:User"] ?? "admin",
        builder.Configuration["Admin:Password"]
            ?? throw new InvalidOperationException("Configure Admin:Password."));
});
```

This in-memory user store is intended for development and demonstrations. Production
applications should register an `IAdminUserStore` backed by persistent storage before
calling `AddLaranetAdminAuthentication`. The login is available at `/admin/login` and
uses a protected `Laranet.Admin` cookie.

```csharp
builder.Services.AddLaranetAdmin();
builder.Services.AddRazorPages();

app.MapRazorPages();
```
