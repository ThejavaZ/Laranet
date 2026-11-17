using Laranet.Admin;
using Laranet.Samples.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Samples.Admin.Data;

public sealed class ProductAdminAdapter(AdminDbContext db) : IAdminDataAdapter
{
    // The typed resource caches its Definition per instance; the registry owns
    // its own instance, so comparisons fall back to the resource name below.
    private static readonly AdminResource Definition = new ProductAdminResource().Definition;

    public async Task<AdminPage> ListAsync(
        AdminResource resource,
        AdminQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureResource(resource);
        var normalized = query.Normalize();
        var products = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            products = products.Where(product =>
                product.Name.Contains(normalized.Search));
        }

        if (normalized.Filters?.TryGetValue("IsActive", out var active) == true
            && bool.TryParse(active, out var isActive))
        {
            products = products.Where(product => product.IsActive == isActive);
        }

        var total = await products.LongCountAsync(cancellationToken);
        var items = await products
            .OrderBy(product => product.Id)
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(product => Row(product))
            .ToListAsync(cancellationToken);

        return new AdminPage(items, normalized.Page, normalized.PageSize, total);
    }

    public async Task<IReadOnlyDictionary<string, object?>?> ReadAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default)
    {
        EnsureResource(resource);
        var product = await db.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == Convert.ToInt32(key), cancellationToken);

        return product is null ? null : Row(product);
    }

    public async Task<IReadOnlyDictionary<string, object?>> CreateAsync(
        AdminResource resource,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default)
    {
        EnsureResource(resource);
        var product = new Product();
        Apply(product, values);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return Row(product);
    }

    public async Task<IReadOnlyDictionary<string, object?>> UpdateAsync(
        AdminResource resource,
        object key,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default)
    {
        EnsureResource(resource);
        var product = await db.Products
            .SingleAsync(item => item.Id == Convert.ToInt32(key), cancellationToken);
        Apply(product, values);
        await db.SaveChangesAsync(cancellationToken);

        return Row(product);
    }

    public async Task DeleteAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default)
    {
        EnsureResource(resource);
        var product = await db.Products
            .SingleAsync(item => item.Id == Convert.ToInt32(key), cancellationToken);
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, object?> Row(Product product)
    {
        return new Dictionary<string, object?>
        {
            ["Id"] = product.Id,
            ["Name"] = product.Name,
            ["Price"] = product.Price,
            ["IsActive"] = product.IsActive,
        };
    }

    private static void Apply(Product product, IReadOnlyDictionary<string, object?> values)
    {
        if (values.TryGetValue("Name", out var name))
        {
            product.Name = Convert.ToString(name) ?? string.Empty;
        }

        if (values.TryGetValue("Price", out var price))
        {
            product.Price = Convert.ToDecimal(price);
        }

        if (values.TryGetValue("IsActive", out var active))
        {
            product.IsActive = Convert.ToBoolean(active);
        }
    }

    private static void EnsureResource(AdminResource resource)
    {
        if (!ReferenceEquals(resource, Definition)
            && !string.Equals(resource.Name, Definition.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException("This adapter only supports the Products resource.", nameof(resource));
        }
    }
}
