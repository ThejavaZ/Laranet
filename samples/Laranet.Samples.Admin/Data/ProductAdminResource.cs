using Laranet.Admin;
using Laranet.Samples.Admin.Models;

namespace Laranet.Samples.Admin.Data;

public static class ProductAdminResource
{
    public static AdminResource Definition { get; } = new(
        "Products",
        typeof(Product),
        [
            new AdminField("Id", "ID", typeof(int), IsKey: true, IsReadOnly: true, Section: "Product"),
            new AdminField("Name", "Name", typeof(string), IsSearchable: true, IsOrderable: true, Section: "Product"),
            new AdminField("Price", "Price", typeof(decimal), IsOrderable: true, Section: "Pricing"),
            new AdminField("IsActive", "Active", typeof(bool), IsOrderable: true, Section: "Pricing"),
        ],
        [
            new AdminFilter("Name", "Name", typeof(string)),
            new AdminFilter("IsActive", "Active", typeof(bool)),
        ],
        [
            new AdminAction("Create", "Create product", MutatesData: true),
            new AdminAction("Edit", "Edit product", MutatesData: true),
            new AdminAction("Delete", "Delete product", MutatesData: true),
        ],
        "Admin.Products",
        Singular: "Product",
        Icon: "box");
}
