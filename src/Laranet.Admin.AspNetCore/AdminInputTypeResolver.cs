using Laranet.Admin;

namespace Laranet.Admin.AspNetCore;

internal static class AdminInputTypeResolver
{
    public static string Resolve(AdminField field)
    {
        if (field.FieldType != AdminFieldType.Auto)
        {
            return field.FieldType switch
            {
                AdminFieldType.TextArea => "text",
                AdminFieldType.Number => "number",
                AdminFieldType.Date => "date",
                AdminFieldType.DateTime => "datetime-local",
                AdminFieldType.Checkbox => "checkbox",
                AdminFieldType.File => "file",
                AdminFieldType.Hidden => "hidden",
                _ => "text",
            };
        }

        var type = Nullable.GetUnderlyingType(field.Type) ?? field.Type;

        if (type == typeof(bool))
        {
            return "checkbox";
        }

        if (type == typeof(DateOnly))
        {
            return "date";
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return field.Name.Contains("Time", StringComparison.OrdinalIgnoreCase)
                ? "datetime-local"
                : "date";
        }

        if (type == typeof(int) || type == typeof(long)
            || type == typeof(decimal) || type == typeof(double)
            || type == typeof(float))
        {
            return "number";
        }

        return "text";
    }

    public static string? ResolveStep(AdminField field)
    {
        if (!string.IsNullOrEmpty(field.Step))
        {
            return field.Step;
        }

        var type = Nullable.GetUnderlyingType(field.Type) ?? field.Type;

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return "any";
        }

        return null;
    }
}
