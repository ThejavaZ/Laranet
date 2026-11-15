namespace Laranet.Cli.Generation;

internal static class AdminResourceTemplates
{
    public static string Render(
        string @namespace,
        string resourceName,
        string entityName,
        IReadOnlyList<CrudField> fields,
        string policyName)
    {
        var fieldLines = fields.Count == 0
            ? "        // Add fields here."
            : string.Join(
                "\n",
                fields.Select(field =>
                    $"        resource.Field(model => model.{field.Name})" +
                    (field.IsString ? ".Searchable()" : string.Empty) +
                    ";"));

        return $$"""
            using Laranet.Admin;

            namespace {{@namespace}};

            public sealed class {{resourceName}} : AdminResource<{{entityName}}>
            {
                protected override void Configure(AdminResourceBuilder<{{entityName}}> resource)
                {
            {{fieldLines}}
                    resource.Permission("{{policyName}}");
                }
            }
            """;
    }
}
