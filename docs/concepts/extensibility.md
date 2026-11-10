# Extensibility

Laranet is designed to be extended without being forked. Extension points are small,
explicit, and built on native .NET mechanisms.

## Extension methods

The primary extension point is a normal extension method over `IQueryable<T>`. Custom
operations compose with the built-in ones because they all return standard queryables.

```csharp
public static IQueryable<T> WhereActive<T>(
    this IQueryable<T> source)
    where T : IActivatable
{
    return source.Where(entity => entity.IsActive);
}
```

```csharp
var page = db.Products
    .WhereActive()
    .Search(product => product.Name, request.Search)
    .Paginate(request.Page, request.PageSize);
```

Guidelines:

- Build expression trees; do not materialize the query.
- Do not switch to in-memory execution silently.
- Keep provider-specific behavior in an integration package.
- Validate public arguments and document exceptions.

## Custom CLI templates

The CLI accepts user-defined templates through a `.laranet/templates` directory. The
nearest directory wins and overrides the built-in template with the same name.

```text
.laranet/
└── templates/
    ├── model.tpl
    ├── service.tpl
    ├── controller.tpl
    ├── middleware.tpl
    └── repository.tpl
```

Templates support `{{Name}}` and `{{Namespace}}`. The folder, file name, and namespace
stay convention-based, so a template changes the content without breaking the
structure.

See the [CLI guide](../guides/cli.md#custom-templates) for a complete example.

## Adding a package

A new package is admitted only when it satisfies the conditions in the
[ecosystem architecture](../architecture/ecosystem.md#9-module-admission-gates):

- A concrete, documented use case exists.
- It has a responsibility that does not fit an existing module.
- Its dependency direction is valid.
- It can be consumed independently.
- It has tests, XML documentation, and an accepted maintenance cost.
- An ADR records the decision when it affects architecture or public contracts.

Proposals that only wrap an existing .NET API without a demonstrated gap are not
accepted.

## Providing an alternative data adapter

`Laranet.Data` is provider-neutral. A future adapter for Dapper, ADO.NET, or another
query provider would implement the same contracts and be distributed as its own
package. It would not change the behavior of `Laranet.Data` or add a dependency to it.

## Replacing a module

Because packages are independent, an application can stop using one without
rewriting the others:

- Replace `Laranet.EntityFrameworkCore` with native EF Core calls and keep
  `Laranet.Data`.
- Keep `Laranet.Data` and drop the ASP.NET Core integration.
- Remove the CLI after generating code; generated files are ordinary C#.

There is no runtime metadata that ties generated code to Laranet.

## Related

- [Core concepts](core-concepts.md)
- [Dependency injection](dependency-injection.md)
- [Module admission gates](../architecture/ecosystem.md#9-module-admission-gates)
- [ADR index](../architecture/decisions/README.md)
