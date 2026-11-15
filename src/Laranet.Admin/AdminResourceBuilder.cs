using System.Linq.Expressions;

namespace Laranet.Admin;

/// <summary>
/// Configures provider-neutral metadata for an administrative resource.
/// </summary>
/// <typeparam name="TModel">The model represented by the resource.</typeparam>
public sealed class AdminResourceBuilder<TModel>
{
    private readonly List<AdminField> fields = [];
    private readonly List<AdminFilter> filters = [];
    private readonly List<AdminAction> actions = [];
    private string? permissionPolicy;
    private string? defaultOrder;
    private bool orderDescending;

    /// <summary>
    /// Adds a model field to the resource.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Field<TValue>(
        Expression<Func<TModel, TValue>> selector,
        string? label = null)
    {
        var name = PropertyName(selector);
        var property = typeof(TModel).GetProperty(name)
            ?? throw new ArgumentException(
                $"Property '{name}' does not exist on {typeof(TModel).Name}.",
                nameof(selector));
        var index = fields.FindIndex(field => field.Name == name);
        var field = new AdminField(
            name,
            label ?? name,
            property.PropertyType,
            IsKey: name.Equals("Id", StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            fields[index] = field;
            index = fields.Count - 1;
        }
        else
        {
            fields.Add(field);
            index = fields.Count - 1;
        }

        return new AdminFieldBuilder<TModel, TValue>(this, index);
    }

    /// <summary>
    /// Adds a filter based on a model property.
    /// </summary>
    public void Filter<TValue>(
        Expression<Func<TModel, TValue>> selector,
        string? label = null,
        bool range = false)
    {
        var name = PropertyName(selector);
        var property = typeof(TModel).GetProperty(name)
            ?? throw new ArgumentException(
                $"Property '{name}' does not exist on {typeof(TModel).Name}.",
                nameof(selector));

        filters.Add(new AdminFilter(name, label ?? name, property.PropertyType, range));
        EnsureField(name, label);
    }

    /// <summary>
    /// Adds a custom action to the resource.
    /// </summary>
    public void Action(string name, string label, bool mutatesData)
    {
        actions.Add(new AdminAction(name, label, mutatesData));
    }

    /// <summary>
    /// Sets the permission policy used by the resource.
    /// </summary>
    public void Permission(string policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        permissionPolicy = policy;
    }

    /// <summary>
    /// Sets the default ordering field.
    /// </summary>
    public void OrderBy<TValue>(
        Expression<Func<TModel, TValue>> selector,
        bool descending = false)
    {
        defaultOrder = PropertyName(selector);
        orderDescending = descending;
        EnsureField(defaultOrder, defaultOrder);
        var field = fields.First(item => item.Name == defaultOrder);
        fields[fields.IndexOf(field)] = field with { IsOrderable = true };
    }

    internal AdminResource Build(string? name = null)
    {
        var resourceName = name ?? typeof(TModel).Name + "s";
        var formFields = fields.Where(field => !field.IsReadOnly).ToArray();

        return new AdminResource(
            resourceName,
            typeof(TModel),
            fields,
            filters,
            actions,
            permissionPolicy,
            new AdminForm(formFields),
            defaultOrder,
            orderDescending);
    }

    internal void MarkSearchable(int index)
    {
        var field = fields[index];
        fields[index] = field with { IsSearchable = true };
    }

    internal void MarkFilterable(int index)
    {
        var field = fields[index];
        fields[index] = field with { IsFilterable = true };
    }

    internal void MarkOrderable(int index)
    {
        var field = fields[index];
        fields[index] = field with { IsOrderable = true };
    }

    internal void SetFormat(int index, string format)
    {
        var field = fields[index];
        fields[index] = field with { Format = format };
    }

    private void EnsureField(string name, string? label)
    {
        if (fields.Any(field => field.Name == name))
        {
            return;
        }

        var property = typeof(TModel).GetProperty(name)
            ?? throw new ArgumentException(
                $"Property '{name}' does not exist on {typeof(TModel).Name}.",
                nameof(name));
        fields.Add(new AdminField(
            name,
            label ?? name,
            property.PropertyType,
            IsKey: name.Equals("Id", StringComparison.OrdinalIgnoreCase)));
    }

    private static string PropertyName<TValue>(Expression<Func<TModel, TValue>> selector)
    {
        var expression = selector.Body is UnaryExpression unary
            ? unary.Operand
            : selector.Body;

        return expression is MemberExpression { Member: System.Reflection.PropertyInfo property }
            ? property.Name
            : throw new ArgumentException(
                "The selector must reference a model property.",
                nameof(selector));
    }
}

/// <summary>
/// Configures display behavior for one resource field.
/// </summary>
/// <typeparam name="TModel">The resource model.</typeparam>
/// <typeparam name="TValue">The field value type.</typeparam>
public sealed class AdminFieldBuilder<TModel, TValue>
{
    private readonly AdminResourceBuilder<TModel> owner;
    private readonly int index;

    internal AdminFieldBuilder(AdminResourceBuilder<TModel> owner, int index)
    {
        this.owner = owner;
        this.index = index;
    }

    /// <summary>
    /// Marks the field as searchable.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Searchable()
    {
        owner.MarkSearchable(index);
        return this;
    }

    /// <summary>
    /// Marks the field as filterable.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Filterable()
    {
        owner.MarkFilterable(index);
        return this;
    }

    /// <summary>
    /// Marks the field as orderable.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Orderable()
    {
        owner.MarkOrderable(index);
        return this;
    }

    /// <summary>
    /// Sets a display format hint for the UI integration.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> FormatAs(string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        owner.SetFormat(index, format);
        return this;
    }
}

/// <summary>
/// Declares a typed administrative resource.
/// </summary>
/// <typeparam name="TModel">The model represented by the resource.</typeparam>
public abstract class AdminResource<TModel>
{
    private AdminResource? definition;

    /// <summary>
    /// Gets the built provider-neutral definition.
    /// </summary>
    public AdminResource Definition => definition ??= BuildDefinition();

    /// <summary>
    /// Configures the resource metadata.
    /// </summary>
    protected abstract void Configure(AdminResourceBuilder<TModel> resource);

    /// <summary>
    /// Gets the resource name used by the admin routes.
    /// </summary>
    protected virtual string ResourceName => typeof(TModel).Name + "s";

    private AdminResource BuildDefinition()
    {
        var builder = new AdminResourceBuilder<TModel>();
        Configure(builder);

        return builder.Build(ResourceName);
    }
}
