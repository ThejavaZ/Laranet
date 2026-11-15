using Laranet.Admin;

namespace Laranet.Admin.AspNetCore;

internal static class AdminInputTypeResolver
{
    public static string Resolve(AdminField field)
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
            _ => field.Type == typeof(bool) ? "checkbox" : "text",
        };
    }
}
