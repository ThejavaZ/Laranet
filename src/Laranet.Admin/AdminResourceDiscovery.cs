using System.Reflection;

namespace Laranet.Admin;

/// <summary>
/// Marks a class as an administrative resource for optional auto-discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AdminResourceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the resource name. Defaults to the type name + "s".
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the permission policy name.
    /// </summary>
    public string? PermissionPolicy { get; set; }
}

/// <summary>
/// Provides optional auto-discovery of admin resources from an assembly.
/// </summary>
public static class AdminResourceDiscovery
{
    /// <summary>
    /// Discovers all types decorated with <see cref="AdminResourceAttribute"/>
    /// in the specified assembly.
    /// </summary>
    public static IReadOnlyList<Type> Discover(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes()
            .Where(type => !type.IsAbstract
                           && type.GetCustomAttribute<AdminResourceAttribute>() is not null
                           && typeof(AdminResource).IsAssignableFrom(type))
            .ToArray();
    }

    /// <summary>
    /// Registers all discovered resources into the registry.
    /// </summary>
    public static void RegisterAll(
        IAdminResourceRegistry registry,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in Discover(assembly))
        {
            var instance = Activator.CreateInstance(type);

            if (instance is not null)
            {
                var definitionProperty = type.GetProperty("Definition");

                if (definitionProperty?.GetValue(instance) is AdminResource definition)
                {
                    registry.Add(definition);
                }
            }
        }
    }
}
