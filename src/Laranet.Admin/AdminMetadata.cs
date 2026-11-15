namespace Laranet.Admin;

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
    string? Format = null);

/// <summary>
/// Describes a filter supported by an administrative resource.
/// </summary>
public sealed record AdminFilter(
    string Name,
    string Label,
    Type ValueType,
    bool IsRange = false);

/// <summary>
/// Describes an explicit action available on an administrative resource.
/// </summary>
public sealed record AdminAction(string Name, string Label, bool MutatesData);

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
    bool DefaultOrderDescending = false)
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
}
