using Laranet.Admin;
using Laranet.Samples.Admin.Models;

namespace Laranet.Samples.Admin.Data;

/// <summary>
/// The panel's Product resource. Fields are inferred from <see cref="Product"/>'s
/// public properties and DataAnnotations (ADR-0015); this class only declares
/// what inference cannot know: search, ordering, format, filters, actions,
/// permissions, and the icon.
/// </summary>
public sealed class ProductAdminResource : AdminResource<Product>
{
    protected override void Configure(AdminResourceBuilder<Product> resource)
    {
        resource.Field(product => product.Name)
            .Searchable()
            .Orderable();
        resource.Field(product => product.Price)
            .Orderable()
            .FormatAs("currency");
        resource.Field(product => product.IsActive)
            .Orderable();

        resource.Filter(product => product.Name);
        resource.Filter(product => product.IsActive);

        resource.Action("Create", "Create product", mutatesData: true);
        resource.Action("Edit", "Edit product", mutatesData: true);
        resource.Action("Delete", "Delete product", mutatesData: true);

        resource.Permission("Admin.Products");
    }

    protected override string? Icon => "box";
}
