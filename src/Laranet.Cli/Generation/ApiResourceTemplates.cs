namespace Laranet.Cli.Generation;

internal static class ApiResourceTemplates
{
    public static string Render(
        string @namespace,
        string resourceName,
        string entityName,
        IReadOnlyList<CrudField> fields)
    {
        var properties = fields.Count == 0
            ? string.Empty
            : string.Join(
                "\n",
                fields.Select(field =>
                    $"    public {field.Declaration} {field.Name} {{ get; init; }}{(field.RequiresDefault ? " = string.Empty;" : string.Empty)}"));

        var initializers = fields.Count == 0
            ? string.Empty
            : string.Join(
                "\n",
                fields.Select(field =>
                    $"        {field.Name} = entity.{field.Name},"));

        var fromEntity = fields.Count == 0
            ? $"        return new {resourceName}();"
            : $"        return new {resourceName}\n        {{\n{initializers}\n        }};";

        return $$"""
            namespace {{@namespace}};

            public sealed record {{resourceName}}
            {
            {{properties}}

                public static {{resourceName}} FromEntity({{entityName}} entity)
                {
            {{fromEntity}}
                }
            }

            """;
    }
}
