namespace Laranet.Admin;

/// <summary>
/// Stores the explicitly registered administrative resources.
/// </summary>
public interface IAdminResourceRegistry
{
    /// <summary>
    /// Gets all registered resources in registration order.
    /// </summary>
    IReadOnlyList<AdminResource> Resources { get; }

    /// <summary>
    /// Registers a resource.
    /// </summary>
    void Add(AdminResource resource);

    /// <summary>
    /// Finds a resource by name.
    /// </summary>
    AdminResource Get(string name);
}

/// <summary>
/// Default in-memory resource registry.
/// </summary>
public sealed class AdminResourceRegistry : IAdminResourceRegistry
{
    private readonly List<AdminResource> resources = [];

    /// <inheritdoc />
    public IReadOnlyList<AdminResource> Resources => resources;

    /// <inheritdoc />
    public void Add(AdminResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resources.Any(existing =>
                string.Equals(existing.Name, resource.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"An administrative resource named '{resource.Name}' is already registered.");
        }

        if (!resource.Fields.Any(field => field.IsKey))
        {
            throw new ArgumentException(
                $"Resource '{resource.Name}' must define one key field.",
                nameof(resource));
        }

        if (resource.Fields.Count(field => field.IsKey) > 1)
        {
            throw new ArgumentException(
                $"Resource '{resource.Name}' must define only one key field.",
                nameof(resource));
        }

        resource.Validate();
        resources.Add(resource);
    }

    /// <inheritdoc />
    public AdminResource Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return resources.FirstOrDefault(resource =>
                   string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase))
               ?? throw new KeyNotFoundException(
                   $"No administrative resource named '{name}' is registered.");
    }
}
