using System.Reflection;

namespace Laranet.Admin;

/// <summary>
/// Declarative inputs for <c>AddLaranetAdmin</c>. This type is a provider-neutral
/// data bag: the integration package applies these registrations to the service
/// collection, keeping the core free of ASP.NET Core and EF Core references.
/// </summary>
public sealed class AdminRegistrationOptions
{
    private readonly List<AdminResource> resources = [];
    private readonly List<Assembly> discoveryAssemblies = [];
    private readonly List<Func<IServiceProvider, IAdminDataAdapter>> adapterFactories = [];

    /// <summary>
    /// Gets the explicitly added resource definitions.
    /// </summary>
    public IReadOnlyList<AdminResource> Resources => resources;

    /// <summary>
    /// Gets the assemblies scanned for <see cref="AdminResourceAttribute"/>.
    /// </summary>
    public IReadOnlyList<Assembly> DiscoveryAssemblies => discoveryAssemblies;

    /// <summary>
    /// Gets the factories that provide the data adapter.
    /// </summary>
    public IReadOnlyList<Func<IServiceProvider, IAdminDataAdapter>> AdapterFactories =>
        adapterFactories;

    /// <summary>
    /// Adds an explicit resource definition.
    /// </summary>
    /// <param name="definition">The provider-neutral resource metadata.</param>
    /// <returns>The same options instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> is null.</exception>
    public AdminRegistrationOptions AddResource(AdminResource definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        resources.Add(definition);
        return this;
    }

    /// <summary>
    /// Adds a resource from a class exposing a static or instance
    /// <c>Definition</c> property — either a typed <c>AdminResource&lt;T&gt;</c>
    /// subclass or a static class following the explicit definition pattern.
    /// </summary>
    /// <typeparam name="TResource">The resource class.</typeparam>
    /// <returns>The same options instance, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="TResource"/> does not expose a compatible
    /// <c>Definition</c> property.
    /// </exception>
    public AdminRegistrationOptions AddResource<TResource>()
        where TResource : class
    {
        var type = typeof(TResource);
        var definitionProperty = type.GetProperty("Definition")
            ?? throw new ArgumentException(
                $"{type.Name} must expose a public Definition property.",
                nameof(TResource));

        object? instance = null;
        if (!type.IsAbstract)
        {
            instance = Activator.CreateInstance(type);
        }
        else if (definitionProperty.GetMethod?.IsStatic != true)
        {
            throw new ArgumentException(
                $"{type.Name} is abstract and only exposes an instance Definition property.",
                nameof(TResource));
        }

        if (definitionProperty.GetValue(instance) is not AdminResource definition)
        {
            throw new ArgumentException(
                $"{type.Name}.Definition must be an AdminResource.",
                nameof(TResource));
        }

        return AddResource(AdminResourceDiscovery.ApplyAttributeMetadata(type, definition));
    }

    /// <summary>
    /// Discovers every class marked with <see cref="AdminResourceAttribute"/>
    /// that exposes a <c>Definition</c> property in the given assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The same options instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
    public AdminRegistrationOptions DiscoverResources(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        discoveryAssemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Registers a factory for <see cref="IAdminDataAdapter"/>. Provider packages
    /// contribute sugar over this method (for example <c>UseEfCore&lt;TContext&gt;()</c>).
    /// </summary>
    /// <param name="factory">Factory invoked per scope to resolve the adapter.</param>
    /// <returns>The same options instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
    public AdminRegistrationOptions AddAdapter(
        Func<IServiceProvider, IAdminDataAdapter> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        adapterFactories.Add(factory);
        return this;
    }
}
