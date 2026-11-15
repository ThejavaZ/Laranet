using Laranet.Admin;
using Laranet.Samples.Admin.Models;

namespace Laranet.Samples.Admin.Data;

public static class ProductAdminResource
{
    public static AdminResource Definition { get; } = new(
        "Products",
        typeof(Product),
        [
            new AdminField("Id", "ID", typeof(int), IsKey: true, IsReadOnly: true),
            new AdminField("Name", "Name", typeof(string)),
            new AdminField("Price", "Price", typeof(decimal)),
            new AdminField("IsActive", "Active", typeof(bool)),
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
        "Admin.Products");
}
