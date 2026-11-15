# Laranet.Admin.EntityFrameworkCore

Experimental EF Core adapter for `Laranet.Admin`.

The adapter implements `IAdminDataAdapter` using the application's `DbContext`.
It supports provider-translatable search, filters, ordering, pagination, mapping,
CRUD, and convention-based soft-delete. The core `Laranet.Admin` package remains
independent of EF Core.

```csharp
builder.Services.AddScoped<IAdminDataAdapter, EfCoreAdminDataAdapter>();
```

This package is not part of the current release train. Validate it against your
application before relying on the API.
