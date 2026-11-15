# Laranet.Admin.AspNetCore

Experimental Razor Pages integration for `Laranet.Admin`.

The package provides conventional `/admin/{resource}` pages for listing, searching,
pagination, creating, editing, and deleting resource rows. Applications provide the
`IAdminDataAdapter`, register resources, and configure native ASP.NET Core
authentication and authorization.

It does not include EF Core, Identity, a database provider, a custom view engine, or
frontend components. The API is experimental and is not part of the current release
train.

```csharp
builder.Services.AddLaranetAdmin();
builder.Services.AddRazorPages();

app.MapRazorPages();
```
