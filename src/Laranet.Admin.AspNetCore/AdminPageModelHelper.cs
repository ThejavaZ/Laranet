using Laranet.Admin;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Shared helper for admin page models.
/// </summary>
internal static class AdminPageModelHelper
{
    public static string L(IStringLocalizer localizer, AdminField field) =>
        field.LocalizationKey is not null
            ? localizer[field.LocalizationKey]
            : field.Label;

    public static string L(IStringLocalizer localizer, AdminFilter filter) =>
        filter.LocalizationKey is not null
            ? localizer[filter.LocalizationKey]
            : filter.Label;

    public static string L(IStringLocalizer localizer, AdminAction action) =>
        action.LocalizationKey is not null
            ? localizer[action.LocalizationKey]
            : action.Label;

    public static string FormatValue(AdminField field, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return field.Format switch
        {
            "currency" => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "{0:C}",
                value),
            "date" => value is DateTime dt
                ? dt.ToString("d", System.Globalization.CultureInfo.CurrentCulture)
                : value.ToString() ?? string.Empty,
            "datetime" => value is DateTime dt2
                ? dt2.ToString("g", System.Globalization.CultureInfo.CurrentCulture)
                : value.ToString() ?? string.Empty,
            "number" => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "{0:N}",
                value),
            _ => value.ToString() ?? string.Empty,
        };
    }

    public static string SingularName(AdminResource resource) =>
        resource.SingularName;

    public static IReadOnlyDictionary<string, string?> ReadFilters(AdminResource resource, Microsoft.AspNetCore.Http.HttpRequest request)
    {
        return resource.Filters
            .Select(filter => new
            {
                filter.Name,
                Value = request.Query[filter.Name].FirstOrDefault(),
            })
            .Where(item => item.Value is not null)
            .ToDictionary(item => item.Name, item => item.Value);
    }
}
