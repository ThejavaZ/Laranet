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

    private const string MiddlewareTemplate = """
        using Microsoft.AspNetCore.Http;

        namespace {{Namespace}};

        public sealed class {{Name}}
        {
            private readonly RequestDelegate _next;

            public {{Name}}(RequestDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                await _next(context);
            }
        }

        """;

    private const string RepositoryTemplate = """
        namespace {{Namespace}};

        /// <summary>
        /// Optional repository scaffold. Use it only when the abstraction is justified.
        /// </summary>
        public sealed class {{Name}}
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

    public static string Middleware(string @namespace, string name)
    {
        return Render(MiddlewareTemplate, @namespace, name);
    }

    public static string Repository(string @namespace, string name)
    {
        return Render(RepositoryTemplate, @namespace, name);
    }

    private static string Render(string template, string @namespace, string name)
    {
        return template
            .Replace("{{Namespace}}", @namespace, StringComparison.Ordinal)
            .Replace("{{Name}}", name, StringComparison.Ordinal);
    }
}
