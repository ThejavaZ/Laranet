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
    /// that expose a <c>Definition</c> property (typed <c>AdminResource&lt;T&gt;</c>
    /// classes or static classes with a <c>Definition</c> property).
    /// </summary>
    public static IReadOnlyList<Type> Discover(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AdminResourceAttribute>() is not null
                           && type.GetProperty("Definition") is not null)
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
            var definitionProperty = type.GetProperty("Definition");
            if (definitionProperty is null)
            {
                continue;
            }

            object? instance = null;
            if (!type.IsAbstract)
            {
                instance = Activator.CreateInstance(type);
            }
            else if (definitionProperty.GetMethod?.IsStatic != true)
            {
                // An abstract class cannot expose an instance definition.
                continue;
            }

            if (definitionProperty.GetValue(instance) is AdminResource definition)
            {
                registry.Add(ApplyAttributeMetadata(type, definition));
            }
        }
    }

    /// <summary>
    /// Applies registration metadata from <see cref="AdminResourceAttribute"/>
    /// (name and permission policy) when it is explicitly set, so both explicit
    /// and discovered registrations honor the attribute contract.
    /// </summary>
    internal static AdminResource ApplyAttributeMetadata(
        Type type,
        AdminResource definition)
    {
        var attribute = type.GetCustomAttribute<AdminResourceAttribute>();
        if (attribute is null)
        {
            return definition;
        }

        return definition with
        {
            Name = string.IsNullOrWhiteSpace(attribute.Name)
                ? definition.Name
                : attribute.Name,
            PermissionPolicy = attribute.PermissionPolicy
                ?? definition.PermissionPolicy,
        };
    }
}
