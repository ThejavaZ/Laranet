# Laranet Admin prototype

This sample validates Sprint 20's first administrative use case:

- `Product` resource.
- SQLite persistence through an EF Core adapter.
- Razor Pages UI.
- ASP.NET Core Identity authentication.
- List, search, pagination, create, edit, and delete operations.

Run it with:

```bash
dotnet run --project samples/Laranet.Samples.Admin
```

The sample is intentionally explicit. It demonstrates the provider-neutral
`Laranet.Admin` contracts without hiding EF Core, Razor Pages, or Identity behind
a new framework.

The admin UI assets are served from the `Laranet.Admin.AspNetCore` package. The
consuming application only needs the standard `app.UseStaticFiles()` middleware.
