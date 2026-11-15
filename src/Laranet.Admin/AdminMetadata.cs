namespace Laranet.Admin;

/// <summary>
/// Identifies the input control rendered for an admin field.
/// </summary>
/// <summary>
/// Identifies the input control rendered for an admin field.
/// </summary>
public enum AdminFieldType
{
    /// <summary>Infers the input type from the .NET type.</summary>
    Auto,
    /// <summary>Renders a single-line text input.</summary>
    Text,
    /// <summary>Renders a multi-line textarea.</summary>
    TextArea,
    /// <summary>Renders a numeric input.</summary>
    Number,
    /// <summary>Renders a date input.</summary>
    Date,
    /// <summary>Renders a date and time input.</summary>
    DateTime,
    /// <summary>Renders a checkbox.</summary>
    Checkbox,
    /// <summary>Renders a select dropdown.</summary>
    Select,
    /// <summary>Renders a file input.</summary>
    File,
    /// <summary>Renders a hidden input.</summary>
    Hidden,
}

/// <summary>
/// Describes a field displayed or edited by an administrative resource.
/// </summary>
public sealed record AdminField(
    string Name,
    string Label,
    Type Type,
    bool IsKey = false,
    bool IsReadOnly = false,
    bool IsSearchable = false,
    bool IsFilterable = false,
    bool IsOrderable = false,
    string? Format = null,
    AdminFieldType FieldType = AdminFieldType.Auto,
    bool IsRequired = false,
    int? MaxLength = null,
    int? MinLength = null,
    string? Min = null,
    string? Max = null,
    string? Step = null,
    string? Placeholder = null,
    string? HelpText = null,
    string? DefaultValue = null,
    int? Rows = null,
    string? LocalizationKey = null);

/// <summary>
/// Describes a filter supported by an administrative resource.
/// </summary>
public sealed record AdminFilter(string Name, string Label, Type ValueType, bool IsRange = false, string? LocalizationKey = null);

/// <summary>
/// Describes an explicit action available on an administrative resource.
/// </summary>
public sealed record AdminAction(string Name, string Label, bool MutatesData, string? LocalizationKey = null);

/// <summary>
/// Describes a selectable relationship used by an administrative form.
/// </summary>
public sealed record AdminRelation(
    string Name,
    string Label,
    Type RelatedType,
    string KeyProperty = "Id",
    string DisplayProperty = "Name");

/// <summary>
/// Represents one option in a relationship selector.
/// </summary>
public sealed record AdminLookupOption(object Key, string Label);

/// <summary>
/// Describes the fields shown by a resource form.
/// </summary>
public sealed record AdminForm(IReadOnlyList<AdminField> Fields);

/// <summary>
/// Describes the metadata and permission policy for one resource.
/// </summary>
public sealed record AdminResource(
    string Name,
    Type ModelType,
    IReadOnlyList<AdminField> Fields,
    IReadOnlyList<AdminFilter> Filters,
    IReadOnlyList<AdminAction> Actions,
    string? PermissionPolicy = null,
    AdminForm? Form = null,
    string? DefaultOrderField = null,
    bool DefaultOrderDescending = false,
    IReadOnlyList<AdminRelation>? Relations = null,
    Func<object, CancellationToken, Task<string?>>? DeleteValidator = null)
{
    /// <summary>
    /// Gets the field used as the resource key.
    /// </summary>
    public AdminField KeyField => Fields.First(item => item.IsKey);

    /// <summary>
    /// Gets the fields enabled for text search.
    /// </summary>
    public IReadOnlyList<AdminField> SearchFields => Fields
        .Where(item => item.IsSearchable)
        .ToArray();

    /// <summary>
    /// Gets the fields enabled for ordering.
    /// </summary>
    public IReadOnlyList<AdminField> OrderableFields => Fields
        .Where(item => item.IsOrderable)
        .ToArray();

    /// <summary>
    /// Gets the fields that should be projected in list queries.
    /// </summary>
    public IReadOnlyList<string> ProjectedFields => Fields
        .Select(item => item.Name)
        .ToArray();

    /// <summary>
    /// Gets the configured relationships.
    /// </summary>
    public IReadOnlyList<AdminRelation> RelationDefinitions => Relations ?? [];

    /// <summary>
    /// Validates the metadata before it is registered.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The resource is invalid.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Resource name is required.", nameof(Name));
        }

        if (Fields.Count == 0)
        {
            throw new ArgumentException(
                $"Resource '{Name}' must define at least one field.",
                nameof(Fields));
        }

        if (Fields.Count(field => field.IsKey) != 1)
        {
            throw new ArgumentException(
                $"Resource '{Name}' must define exactly one key field.",
                nameof(Fields));
        }

        if (Filters.Any(filter =>
                !Fields.Any(field => field.Name == filter.Name)))
        {
            throw new ArgumentException(
                $"Resource '{Name}' defines a filter for an unknown field.",
                nameof(Filters));
        }

        if ((Relations ?? []).Any(relation =>
                !Fields.Any(field => field.Name == relation.Name)))
        {
            throw new ArgumentException(
                $"Resource '{Name}' defines a relation for an unknown field.",
                nameof(Relations));
        }
    }
}
