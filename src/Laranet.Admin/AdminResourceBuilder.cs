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
    private readonly List<AdminRelation> relations = [];
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
        var fieldType = InferFieldType(property.PropertyType, name);
        var field = new AdminField(
            name,
            label ?? name,
            property.PropertyType,
            IsKey: name.Equals("Id", StringComparison.OrdinalIgnoreCase),
            FieldType: fieldType);
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
    /// Adds a relationship selector based on a foreign-key property.
    /// </summary>
    public void BelongsTo<TValue, TRelated>(
        Expression<Func<TModel, TValue>> foreignKey,
        string label,
        string keyProperty = "Id",
        string displayProperty = "Name")
    {
        var name = PropertyName(foreignKey);
        relations.Add(new AdminRelation(
            name,
            label,
            typeof(TRelated),
            keyProperty,
            displayProperty));
        EnsureField(name, label);
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
            orderDescending,
            relations);
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

    internal void SetFieldType(int index, AdminFieldType fieldType)
    {
        var field = fields[index];
        fields[index] = field with { FieldType = fieldType };
    }

    internal void SetRequired(int index, bool required)
    {
        var field = fields[index];
        fields[index] = field with { IsRequired = required };
    }

    internal void SetMaxLength(int index, int? maxLength)
    {
        var field = fields[index];
        fields[index] = field with { MaxLength = maxLength };
    }

    internal void SetMinLength(int index, int? minLength)
    {
        var field = fields[index];
        fields[index] = field with { MinLength = minLength };
    }

    internal void SetMin(int index, string? min)
    {
        var field = fields[index];
        fields[index] = field with { Min = min };
    }

    internal void SetMax(int index, string? max)
    {
        var field = fields[index];
        fields[index] = field with { Max = max };
    }

    internal void SetStep(int index, string? step)
    {
        var field = fields[index];
        fields[index] = field with { Step = step };
    }

    internal void SetPlaceholder(int index, string? placeholder)
    {
        var field = fields[index];
        fields[index] = field with { Placeholder = placeholder };
    }

    internal void SetHelpText(int index, string? helpText)
    {
        var field = fields[index];
        fields[index] = field with { HelpText = helpText };
    }

    internal void SetDefaultValue(int index, string? defaultValue)
    {
        var field = fields[index];
        fields[index] = field with { DefaultValue = defaultValue };
    }

    internal void SetRows(int index, int? rows)
    {
        var field = fields[index];
        fields[index] = field with { Rows = rows };
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

    private static AdminFieldType InferFieldType(Type type, string name)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(bool))
        {
            return AdminFieldType.Checkbox;
        }

        if (underlying == typeof(DateOnly))
        {
            return AdminFieldType.Date;
        }

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
        {
            return name.Contains("Time", StringComparison.OrdinalIgnoreCase)
                ? AdminFieldType.DateTime
                : AdminFieldType.Date;
        }

        if (underlying == typeof(int) || underlying == typeof(long)
            || underlying == typeof(decimal) || underlying == typeof(double)
            || underlying == typeof(float))
        {
            return AdminFieldType.Number;
        }

        if (underlying.IsEnum)
        {
            return AdminFieldType.Select;
        }

        return AdminFieldType.Text;
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

    /// <summary>
    /// Sets the field input type to date.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsDate()
    {
        owner.SetFieldType(index, AdminFieldType.Date);
        return this;
    }

    /// <summary>
    /// Sets the field input type to date and time.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsDateTime()
    {
        owner.SetFieldType(index, AdminFieldType.DateTime);
        return this;
    }

    /// <summary>
    /// Sets the field input type to number.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsNumber()
    {
        owner.SetFieldType(index, AdminFieldType.Number);
        return this;
    }

    /// <summary>
    /// Sets the field input type to a textarea.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsTextArea(int? rows = null)
    {
        owner.SetFieldType(index, AdminFieldType.TextArea);
        owner.SetRows(index, rows);
        return this;
    }

    /// <summary>
    /// Sets the field input type to checkbox.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsCheckbox()
    {
        owner.SetFieldType(index, AdminFieldType.Checkbox);
        return this;
    }

    /// <summary>
    /// Sets the field input type to file.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsFile()
    {
        owner.SetFieldType(index, AdminFieldType.File);
        return this;
    }

    /// <summary>
    /// Sets the field input type to hidden.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> AsHidden()
    {
        owner.SetFieldType(index, AdminFieldType.Hidden);
        return this;
    }

    /// <summary>
    /// Marks the field as required.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Required()
    {
        owner.SetRequired(index, true);
        return this;
    }

    /// <summary>
    /// Sets the maximum string length.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> MaxLength(int length)
    {
        owner.SetMaxLength(index, length);
        return this;
    }

    /// <summary>
    /// Sets the minimum string length.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> MinLength(int length)
    {
        owner.SetMinLength(index, length);
        return this;
    }

    /// <summary>
    /// Sets the minimum value.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Min(string value)
    {
        owner.SetMin(index, value);
        return this;
    }

    /// <summary>
    /// Sets the maximum value.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Max(string value)
    {
        owner.SetMax(index, value);
        return this;
    }

    /// <summary>
    /// Sets the step increment for numeric inputs.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Step(string value)
    {
        owner.SetStep(index, value);
        return this;
    }

    /// <summary>
    /// Sets the placeholder text.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> Placeholder(string value)
    {
        owner.SetPlaceholder(index, value);
        return this;
    }

    /// <summary>
    /// Sets the help text.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> HelpText(string value)
    {
        owner.SetHelpText(index, value);
        return this;
    }

    /// <summary>
    /// Sets the default value.
    /// </summary>
    public AdminFieldBuilder<TModel, TValue> DefaultValue(string value)
    {
        owner.SetDefaultValue(index, value);
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
