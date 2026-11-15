namespace Laranet.Cli.Generation;

internal static class CqrsTemplates
{
    public static string Request(
        GenerationContext context,
        string @namespace,
        CqrsNames names,
        GenerationSettings settings)
    {
        var kind = names.RequestKind;
        var templateKind = kind == GeneratorKind.Command
            ? GeneratorKind.Command
            : GeneratorKind.Query;
        var template = context.Templates.Find(templateKind);

        if (template is not null)
        {
            return Render(template, @namespace, names.RequestType, names, settings);
        }

        var usingDirective = RequestUsing(settings.Mediator);
        var contract = RequestContract(names, settings.Mediator);

        return $$"""
            {{usingDirective}}
            namespace {{@namespace}};

            public sealed record {{names.RequestType}}{{contract}};

            """;
    }

    public static string Handler(
        GenerationContext context,
        string @namespace,
        CqrsNames names,
        GenerationSettings settings)
    {
        var template = context.Templates.Find(GeneratorKind.Handler);

        if (template is not null)
        {
            return Render(template, @namespace, names.HandlerType, names, settings);
        }

        var requestNamespace = RequestNamespace(context, settings.Profile, names);
        var usingRequest = requestNamespace == @namespace
            ? string.Empty
            : $"using {requestNamespace};\n\n";

        return settings.Mediator switch
        {
            MediatorKind.None => $$"""
                {{usingRequest}}namespace {{@namespace}};

                public sealed class {{names.HandlerType}}
                {
                    public Task HandleAsync(
                        {{names.RequestType}} request,
                        CancellationToken cancellationToken = default)
                    {
                        return Task.CompletedTask;
                    }
                }

                """,
            MediatorKind.MediatR => MediatRHandler(@namespace, usingRequest, names),
            MediatorKind.Wolverine => WolverineHandler(@namespace, usingRequest, names),
            _ => throw new ArgumentOutOfRangeException(nameof(settings.Mediator)),
        };
    }

    public static string Test(
        GenerationContext context,
        string @namespace,
        CqrsNames names,
        GenerationSettings settings)
    {
        var requestNamespace = RequestNamespace(context, settings.Profile, names);
        var usingRequest = requestNamespace == @namespace
            ? string.Empty
            : $"using {requestNamespace};\n\n";

        return $$"""
            using Xunit;
            {{usingRequest}}
            namespace {{@namespace}};

            public sealed class {{names.RequestType}}Tests
            {
                [Fact]
                public void Request_ShouldBeCreatable()
                {
                    Assert.NotNull(new {{names.RequestType}}());
                }
            }

            """;
    }

    private static string RequestUsing(MediatorKind mediator)
    {
        return mediator switch
        {
            MediatorKind.None => string.Empty,
            MediatorKind.MediatR => "using MediatR;\n\n",
            MediatorKind.Wolverine => "using Wolverine;\n\n",
            _ => throw new ArgumentOutOfRangeException(nameof(mediator)),
        };
    }

    private static string RequestContract(CqrsNames names, MediatorKind mediator)
    {
        return mediator switch
        {
            MediatorKind.None => string.Empty,
            MediatorKind.MediatR => names.RequestKind == GeneratorKind.Query
                ? " : IRequest<object?>"
                : " : IRequest<Unit>",
            MediatorKind.Wolverine => names.RequestKind == GeneratorKind.Query
                ? " : IQuery<object?>"
                : " : ICommand",
            _ => throw new ArgumentOutOfRangeException(nameof(mediator)),
        };
    }

    private static string MediatRHandler(
        string @namespace,
        string usingRequest,
        CqrsNames names)
    {
        var responseType = names.RequestKind == GeneratorKind.Query ? "object?" : "Unit";

        return $$"""
            using MediatR;
            {{usingRequest}}
            namespace {{@namespace}};

            public sealed class {{names.HandlerType}}
                : IRequestHandler<{{names.RequestType}}, {{responseType}}>
            {
                public Task<{{responseType}}> Handle(
                    {{names.RequestType}} request,
                    CancellationToken cancellationToken)
                {
                    return Task.FromResult<{{responseType}}>(
                        {{(names.RequestKind == GeneratorKind.Query ? "null" : "Unit.Value")}});
                }
            }

            """;
    }

    private static string WolverineHandler(
        string @namespace,
        string usingRequest,
        CqrsNames names)
    {
        var responseType = names.RequestKind == GeneratorKind.Query ? "object?" : "void";
        var returnStatement = names.RequestKind == GeneratorKind.Query
            ? "return null;"
            : string.Empty;

        return $$"""
            using Wolverine;
            {{usingRequest}}
            namespace {{@namespace}};

            public sealed class {{names.HandlerType}}
            {
                public {{responseType}} Handle({{names.RequestType}} request)
                {
                    {{returnStatement}}
                }
            }

            """;
    }

    private static string RequestNamespace(
        GenerationContext context,
        CqrsProfile profile,
        CqrsNames names)
    {
        var folder = names.RequestKind == GeneratorKind.Query ? "Queries" : "Commands";
        var folders = profile switch
        {
            CqrsProfile.Simple => new[] { folder },
            CqrsProfile.Standard => new[] { "Application", folder },
            CqrsProfile.Clean => new[] { "Application", "Features", folder },
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

        return GeneratorSupport.ResolveTarget(context, folders).Namespace;
    }

    private static string Render(
        string template,
        string @namespace,
        string name,
        CqrsNames names,
        GenerationSettings settings)
    {
        return CodeTemplates.Render(template, @namespace, name)
            .Replace("{{Request}}", names.RequestType, StringComparison.Ordinal)
            .Replace("{{Handler}}", names.HandlerType, StringComparison.Ordinal)
            .Replace("{{Mediator}}", settings.Mediator.ToString(), StringComparison.Ordinal);
    }
}
