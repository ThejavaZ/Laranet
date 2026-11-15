namespace Laranet.Admin;

/// <summary>
/// Describes a field displayed or edited by an administrative resource.
/// </summary>
public sealed record AdminField(
    string Name,
    string Label,
    Type Type,
    bool IsKey = false,
    bool IsReadOnly = false);

/// <summary>
/// Describes a filter supported by an administrative resource.
/// </summary>
public sealed record AdminFilter(string Name, string Label, Type ValueType);

/// <summary>
/// Describes an explicit action available on an administrative resource.
/// </summary>
public sealed record AdminAction(string Name, string Label, bool MutatesData);

/// <summary>
/// Describes the metadata and permission policy for one resource.
/// </summary>
public sealed record AdminResource(
    string Name,
    Type ModelType,
    IReadOnlyList<AdminField> Fields,
    IReadOnlyList<AdminFilter> Filters,
    IReadOnlyList<AdminAction> Actions,
    string? PermissionPolicy = null)
{
    /// <summary>
    /// Gets the field used as the resource key.
    /// </summary>
    public AdminField KeyField => Fields.First(item => item.IsKey);
}
