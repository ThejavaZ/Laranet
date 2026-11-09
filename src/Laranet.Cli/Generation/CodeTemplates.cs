namespace Laranet.Cli.Generation;

/// <summary>
/// Contains the source templates used by the generators.
/// </summary>
internal static class CodeTemplates
{
    private const string ModelTemplate = """
        namespace {{Namespace}};

        public class {{Name}}
        {
            public int Id { get; set; }
        }

        """;

    private const string ServiceTemplate = """
        namespace {{Namespace}};

        public sealed class {{Name}}
        {
        }

        """;

    private const string ControllerTemplate = """
        using Microsoft.AspNetCore.Mvc;

        namespace {{Namespace}};

        [ApiController]
        [Route("api/[controller]")]
        public sealed class {{Name}} : ControllerBase
        {
        }

        """;

    public static string Model(string @namespace, string name)
    {
        return Render(ModelTemplate, @namespace, name);
    }

    public static string Service(string @namespace, string name)
    {
        return Render(ServiceTemplate, @namespace, name);
    }

    public static string Controller(string @namespace, string name)
    {
        return Render(ControllerTemplate, @namespace, name);
    }

    private static string Render(string template, string @namespace, string name)
    {
        return template
            .Replace("{{Namespace}}", @namespace, StringComparison.Ordinal)
            .Replace("{{Name}}", name, StringComparison.Ordinal);
    }
}
