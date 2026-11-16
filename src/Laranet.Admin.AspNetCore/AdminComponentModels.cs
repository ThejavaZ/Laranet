using Laranet.Admin;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Describes a form field rendered by the admin UI.
/// </summary>
/// <param name="Field">The resource field metadata.</param>
/// <param name="Label">The localized field label.</param>
/// <param name="Value">The current field value.</param>
/// <param name="Options">Optional relationship options.</param>
public sealed record AdminFieldView(
    AdminField Field,
    string Label,
    object? Value = null,
    IReadOnlyList<AdminLookupOption>? Options = null);

/// <summary>
/// Describes a select control rendered by the admin UI.
/// </summary>
/// <param name="Name">The form field name.</param>
/// <param name="Options">The available options.</param>
/// <param name="Value">The selected value.</param>
/// <param name="IsRequired">Whether the control is required.</param>
/// <param name="EmptyLabel">The label for the empty option.</param>
public sealed record AdminSelectView(
    string Name,
    IReadOnlyList<AdminLookupOption> Options,
    string? Value = null,
    bool IsRequired = false,
    string? EmptyLabel = null);

/// <summary>
/// Describes a button or link rendered by the admin UI.
/// </summary>
/// <param name="Label">The visible label.</param>
/// <param name="Href">The link target. When null, a button is rendered.</param>
/// <param name="Variant">The visual variant: primary, secondary, or danger.</param>
/// <param name="Type">The button type.</param>
/// <param name="IsDisabled">Whether the control is disabled.</param>
/// <param name="ShowLoading">Whether to enable the loading state.</param>
public sealed record AdminButtonView(
    string Label,
    string? Href = null,
    string Variant = "primary",
    string Type = "button",
    bool IsDisabled = false,
    bool ShowLoading = false);

/// <summary>
/// Describes the table rendered for an admin resource list.
/// </summary>
/// <param name="Resource">The resource metadata.</param>
/// <param name="Results">The current page of rows.</param>
/// <param name="SortBy">The current sort field.</param>
/// <param name="SortDescending">Whether the current sort is descending.</param>
/// <param name="SupportsBulk">Whether bulk selection is supported.</param>
/// <param name="RouteValues">The route values that preserve list state.</param>
/// <param name="Label">Resolves a localized field label.</param>
/// <param name="FormatValue">Formats a field value for display.</param>
public sealed record AdminTableModel(
    AdminResource Resource,
    AdminPage Results,
    string? SortBy,
    bool SortDescending,
    bool SupportsBulk,
    Dictionary<string, string> RouteValues,
    Func<AdminField, string> Label,
    Func<AdminField, object?, string> FormatValue);

/// <summary>
/// Describes the pagination and page-size controls rendered for a list.
/// </summary>
/// <param name="Results">The current page of rows.</param>
/// <param name="RouteValues">The route values that preserve list state.</param>
/// <param name="PageSizes">The page sizes offered by the UI.</param>
public sealed record AdminPaginationModel(
    AdminPage Results,
    Dictionary<string, string> RouteValues,
    IReadOnlyList<int>? PageSizes = null);

/// <summary>
/// Describes an empty state rendered by the admin UI.
/// </summary>
/// <param name="Title">The empty-state title.</param>
/// <param name="Message">The supporting message.</param>
/// <param name="ActionLabel">The optional action label.</param>
/// <param name="ActionHref">The optional action target.</param>
/// <param name="Icon">The optional icon name.</param>
public sealed record AdminEmptyStateView(
    string Title,
    string Message,
    string? ActionLabel = null,
    string? ActionHref = null,
    string? Icon = null);
