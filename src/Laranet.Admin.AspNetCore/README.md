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

It does not include EF Core, Identity, a database provider, a custom view engine, or a
JavaScript framework. The API is experimental and is not part of the current release
train.

```csharp
builder.Services.AddLaranetAdmin();
builder.Services.AddRazorPages();

app.MapRazorPages();
```
