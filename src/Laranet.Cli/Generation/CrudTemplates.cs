using System.Text;

namespace Laranet.Cli.Generation;

/// <summary>
/// Contains the source templates used by <c>make:crud</c>.
/// </summary>
internal static class CrudTemplates
{
    private const string EntityTemplate = """
        namespace {{Namespace}};

        public sealed class {{Entity}}
        {
            public int Id { get; set; }

        {{Properties}}
        }

        """;

    private const string RequestTemplate = """
        {{Usings}}namespace {{Namespace}};

        public sealed class {{Entity}}Request
        {
        {{Properties}}
        }

        """;

    private const string ResponseTemplate = """
        namespace {{Namespace}};

        public sealed record {{Entity}}Response({{Parameters}});

        """;

    private const string SimpleControllerTemplate = """
        {{Usings}}namespace {{Namespace}};

        [ApiController]
        [Route("api/{{Route}}")]
        public sealed class {{Controller}}({{Context}} db) : ControllerBase
        {
            [HttpGet]
            public async Task<ActionResult<IReadOnlyList<{{Response}}>>> GetAll(CancellationToken cancellationToken)
            {
                var items = await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .Select({{EntityVariable}} => new {{Response}}({{ResponseArguments}}))
                    .ToListAsync(cancellationToken);

                return Ok(items);
            }

            [HttpGet("{id:{{IdRoute}}}")]
            public async Task<ActionResult<{{Response}}>> GetById({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .Where({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id)
                    .Select({{EntityVariable}} => new {{Response}}({{ResponseArguments}}))
                    .SingleOrDefaultAsync(cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return NotFound();
                }

                return Ok({{EntityVariable}});
            }

            [HttpPost]
            public async Task<ActionResult<{{Response}}>> Create({{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = new {{Entity}}
                {
        {{Initializers}}
                };

                db.Set<{{Entity}}>().Add({{EntityVariable}});
                await db.SaveChangesAsync(cancellationToken);

                var response = new {{Response}}({{ResponseArguments}});

                return CreatedAtAction(nameof(GetById), new { id = {{EntityVariable}}.{{KeyName}} }, response);
            }

            [HttpPut("{id:{{IdRoute}}}")]
            public async Task<IActionResult> Update({{KeyType}} id, {{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await db.Set<{{Entity}}>()
                    .SingleOrDefaultAsync({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return NotFound();
                }

        {{Assignments}}
                await db.SaveChangesAsync(cancellationToken);

                return NoContent();
            }

            [HttpDelete("{id:{{IdRoute}}}")]
            public async Task<IActionResult> Delete({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await db.Set<{{Entity}}>()
                    .SingleOrDefaultAsync({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return NotFound();
                }

                {{DeleteOperation}}
                await db.SaveChangesAsync(cancellationToken);

                return NoContent();
            }
        }

        """;

    private const string ServiceTemplate = """
        {{Usings}}namespace {{Namespace}};

        public sealed class {{Service}}({{Context}} db)
        {
            public async Task<IReadOnlyList<{{Response}}>> GetAllAsync(CancellationToken cancellationToken)
            {
                return await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .Select({{EntityVariable}} => new {{Response}}({{ResponseArguments}}))
                    .ToListAsync(cancellationToken);
            }

            public async Task<{{Response}}?> GetByIdAsync({{KeyType}} id, CancellationToken cancellationToken)
            {
                return await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .Where({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id)
                    .Select({{EntityVariable}} => new {{Response}}({{ResponseArguments}}))
                    .SingleOrDefaultAsync(cancellationToken);
            }

            public async Task<{{Response}}> CreateAsync({{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = new {{Entity}}
                {
        {{Initializers}}
                };

                db.Set<{{Entity}}>().Add({{EntityVariable}});
                await db.SaveChangesAsync(cancellationToken);

                return new {{Response}}({{ResponseArguments}});
            }

            public async Task<bool> UpdateAsync({{KeyType}} id, {{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await db.Set<{{Entity}}>()
                    .SingleOrDefaultAsync({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return false;
                }

        {{Assignments}}
                await db.SaveChangesAsync(cancellationToken);

                return true;
            }

            public async Task<bool> DeleteAsync({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await db.Set<{{Entity}}>()
                    .SingleOrDefaultAsync({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return false;
                }

                {{DeleteOperation}}
                await db.SaveChangesAsync(cancellationToken);

                return true;
            }
        }

        """;

    private const string ServiceControllerTemplate = """
        {{Usings}}namespace {{Namespace}};

        [ApiController]
        [Route("api/{{Route}}")]
        public sealed class {{Controller}}({{Service}} service) : ControllerBase
        {
            [HttpGet]
            public async Task<ActionResult<IReadOnlyList<{{Response}}>>> GetAll(CancellationToken cancellationToken)
            {
                return Ok(await service.GetAllAsync(cancellationToken));
            }

            [HttpGet("{id:{{IdRoute}}}")]
            public async Task<ActionResult<{{Response}}>> GetById({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await service.GetByIdAsync(id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return NotFound();
                }

                return Ok({{EntityVariable}});
            }

            [HttpPost]
            public async Task<ActionResult<{{Response}}>> Create({{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await service.CreateAsync(request, cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = {{EntityVariable}}.{{KeyName}} }, {{EntityVariable}});
            }

            [HttpPut("{id:{{IdRoute}}}")]
            public async Task<IActionResult> Update({{KeyType}} id, {{Request}} request, CancellationToken cancellationToken)
            {
                if (await service.UpdateAsync(id, request, cancellationToken))
                {
                    return NoContent();
                }

                return NotFound();
            }

            [HttpDelete("{id:{{IdRoute}}}")]
            public async Task<IActionResult> Delete({{KeyType}} id, CancellationToken cancellationToken)
            {
                if (await service.DeleteAsync(id, cancellationToken))
                {
                    return NoContent();
                }

                return NotFound();
            }
        }

        """;

    private const string RepositoryInterfaceTemplate = """
        {{Usings}}namespace {{Namespace}};

        public interface {{RepositoryInterface}}
        {
            Task<IReadOnlyList<{{Entity}}>> GetAllAsync(CancellationToken cancellationToken);

            Task<{{Entity}}?> GetByIdAsync({{KeyType}} id, CancellationToken cancellationToken);

            Task AddAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken);

            Task UpdateAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken);

            Task DeleteAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken);
        }

        """;

    private const string RepositoryTemplate = """
        {{Usings}}namespace {{Namespace}};

        public sealed class {{Repository}}({{Context}} db) : {{RepositoryInterface}}
        {
            public async Task<IReadOnlyList<{{Entity}}>> GetAllAsync(CancellationToken cancellationToken)
            {
                return await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
            }

            public async Task<{{Entity}}?> GetByIdAsync({{KeyType}} id, CancellationToken cancellationToken)
            {
                return await db.Set<{{Entity}}>()
                    .AsNoTracking()
                    .SingleOrDefaultAsync({{EntityVariable}} => {{EntityVariable}}.{{KeyName}} == id, cancellationToken);
            }

            public async Task AddAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken)
            {
                db.Set<{{Entity}}>().Add({{EntityVariable}});
                await db.SaveChangesAsync(cancellationToken);
            }

            public async Task UpdateAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken)
            {
                db.Set<{{Entity}}>().Update({{EntityVariable}});
                await db.SaveChangesAsync(cancellationToken);
            }

            public async Task DeleteAsync({{Entity}} {{EntityVariable}}, CancellationToken cancellationToken)
            {
                {{DeleteOperation}}
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        """;

    private const string CleanServiceTemplate = """
        {{Usings}}namespace {{Namespace}};

        public sealed class {{Service}}({{RepositoryInterface}} repository)
        {
            public async Task<IReadOnlyList<{{Response}}>> GetAllAsync(CancellationToken cancellationToken)
            {
                var items = await repository.GetAllAsync(cancellationToken);

                return items
                    .Select({{EntityVariable}} => new {{Response}}({{ResponseArguments}}))
                    .ToList();
            }

            public async Task<{{Response}}?> GetByIdAsync({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await repository.GetByIdAsync(id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return null;
                }

                return new {{Response}}({{ResponseArguments}});
            }

            public async Task<{{Response}}> CreateAsync({{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = new {{Entity}}
                {
        {{Initializers}}
                };

                await repository.AddAsync({{EntityVariable}}, cancellationToken);

                return new {{Response}}({{ResponseArguments}});
            }

            public async Task<bool> UpdateAsync({{KeyType}} id, {{Request}} request, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await repository.GetByIdAsync(id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return false;
                }

        {{Assignments}}
                await repository.UpdateAsync({{EntityVariable}}, cancellationToken);

                return true;
            }

            public async Task<bool> DeleteAsync({{KeyType}} id, CancellationToken cancellationToken)
            {
                var {{EntityVariable}} = await repository.GetByIdAsync(id, cancellationToken);

                if ({{EntityVariable}} is null)
                {
                    return false;
                }

                await repository.DeleteAsync({{EntityVariable}}, cancellationToken);

                return true;
            }
        }

        """;

    private const string RegistrationTemplate = """
        {{Usings}}namespace {{Namespace}};

        public static class {{Entity}}ServiceCollectionExtensions
        {
            public static IServiceCollection Add{{Entity}}Crud(this IServiceCollection services)
            {
        {{Registrations}}
                return services;
            }
        }

        """;

    /// <summary>
    /// Generates the entity.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string Entity(CrudModel model)
    {
        return Render(
            EntityTemplate,
            ("{{Namespace}}", model.EntityNamespace),
            ("{{Entity}}", model.Entity),
            ("{{Properties}}", EntityProperties(model)));
    }

    /// <summary>
    /// Generates the request DTO.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string Request(CrudModel model)
    {
        return Render(
            RequestTemplate,
            ("{{Usings}}", RequestUsings(model)),
            ("{{Namespace}}", model.ContractsNamespace),
            ("{{Entity}}", model.Entity),
            ("{{Properties}}", RequestProperties(model)));
    }

    /// <summary>
    /// Generates the response DTO.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string Response(CrudModel model)
    {
        return Render(
            ResponseTemplate,
            ("{{Namespace}}", model.ContractsNamespace),
            ("{{Entity}}", model.Entity),
            ("{{Parameters}}", ResponseParameters(model)));
    }

    /// <summary>
    /// Generates a controller that uses the context directly.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string SimpleController(CrudModel model)
    {
        return Render(
            SimpleControllerTemplate,
            ("{{Usings}}", Usings(
                model.ControllerNamespace,
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.EntityFrameworkCore",
                model.EntityNamespace,
                model.ContractsNamespace)),
            ("{{Namespace}}", model.ControllerNamespace),
            ("{{Controller}}", model.Controller),
            ("{{Context}}", model.DbContextReference),
            ("{{Entity}}", model.Entity),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{Request}}", model.Request),
            ("{{Response}}", model.Response),
            ("{{Route}}", model.Route),
            ("{{Initializers}}", Initializers(model)),
            ("{{Assignments}}", Assignments(model)),
            ("{{ResponseArguments}}", ResponseArguments(model)),
            ("{{IdRoute}}", RouteConstraint(model.KeyType)),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName),
            ("{{DeleteOperation}}", DeleteOperation(model)));
    }

    /// <summary>
    /// Generates a service that uses the context directly.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string Service(CrudModel model)
    {
        return Render(
            ServiceTemplate,
            ("{{Usings}}", Usings(
                model.ServiceNamespace,
                "Microsoft.EntityFrameworkCore",
                model.EntityNamespace,
                model.ContractsNamespace)),
            ("{{Namespace}}", model.ServiceNamespace),
            ("{{Service}}", model.Service),
            ("{{Context}}", model.DbContextReference),
            ("{{Entity}}", model.Entity),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{Request}}", model.Request),
            ("{{Response}}", model.Response),
            ("{{Initializers}}", Initializers(model)),
            ("{{Assignments}}", Assignments(model)),
            ("{{ResponseArguments}}", ResponseArguments(model)),
            ("{{IdRoute}}", RouteConstraint(model.KeyType)),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName),
            ("{{DeleteOperation}}", DeleteOperation(model)));
    }

    /// <summary>
    /// Generates a controller that delegates to a service.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string ServiceController(CrudModel model)
    {
        return Render(
            ServiceControllerTemplate,
            ("{{Usings}}", Usings(
                model.ControllerNamespace,
                "Microsoft.AspNetCore.Mvc",
                model.ServiceNamespace,
                model.ContractsNamespace)),
            ("{{Namespace}}", model.ControllerNamespace),
            ("{{Controller}}", model.Controller),
            ("{{Service}}", model.Service),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{Request}}", model.Request),
            ("{{Response}}", model.Response),
            ("{{Route}}", model.Route),
            ("{{IdRoute}}", RouteConstraint(model.KeyType)),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName));
    }

    /// <summary>
    /// Generates the repository interface used by the clean profile.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string RepositoryInterface(CrudModel model)
    {
        return Render(
            RepositoryInterfaceTemplate,
            ("{{Usings}}", Usings(
                model.RepositoryInterfaceNamespace,
                model.EntityNamespace)),
            ("{{Namespace}}", model.RepositoryInterfaceNamespace),
            ("{{RepositoryInterface}}", model.RepositoryInterface),
            ("{{Entity}}", model.Entity),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName));
    }

    /// <summary>
    /// Generates the EF Core repository used by the clean profile.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string Repository(CrudModel model)
    {
        return Render(
            RepositoryTemplate,
            ("{{Usings}}", Usings(
                model.RepositoryNamespace,
                "Microsoft.EntityFrameworkCore",
                model.RepositoryInterfaceNamespace,
                model.EntityNamespace)),
            ("{{Namespace}}", model.RepositoryNamespace),
            ("{{Repository}}", model.Repository),
            ("{{RepositoryInterface}}", model.RepositoryInterface),
            ("{{Context}}", model.DbContextReference),
            ("{{Entity}}", model.Entity),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName),
            ("{{DeleteOperation}}", DeleteOperation(model)));
    }

    /// <summary>
    /// Generates the service used by the clean profile.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <returns>The source content.</returns>
    public static string CleanService(CrudModel model)
    {
        return Render(
            CleanServiceTemplate,
            ("{{Usings}}", Usings(
                model.ServiceNamespace,
                model.RepositoryInterfaceNamespace,
                model.EntityNamespace,
                model.ContractsNamespace)),
            ("{{Namespace}}", model.ServiceNamespace),
            ("{{Service}}", model.Service),
            ("{{RepositoryInterface}}", model.RepositoryInterface),
            ("{{Entity}}", model.Entity),
            ("{{EntityVariable}}", model.EntityVariable),
            ("{{Request}}", model.Request),
            ("{{Response}}", model.Response),
            ("{{Initializers}}", Initializers(model)),
            ("{{Assignments}}", Assignments(model)),
            ("{{ResponseArguments}}", ResponseArguments(model)),
            ("{{KeyType}}", model.KeyType),
            ("{{KeyName}}", model.KeyName),
            ("{{DeleteOperation}}", DeleteOperation(model)));
    }

    /// <summary>
    /// Generates the dependency-injection registration extension.
    /// </summary>
    /// <param name="model">The CRUD model.</param>
    /// <param name="registrations">The service registration lines.</param>
    /// <returns>The source content.</returns>
    public static string Registration(
        CrudModel model,
        IReadOnlyList<string> registrations)
    {
        return Render(
            RegistrationTemplate,
            ("{{Usings}}", Usings(
                model.ExtensionsNamespace,
                "Microsoft.Extensions.DependencyInjection",
                model.ServiceNamespace,
                model.RepositoryInterfaceNamespace,
                model.RepositoryNamespace)),
            ("{{Namespace}}", model.ExtensionsNamespace),
            ("{{Entity}}", model.Entity),
            ("{{Registrations}}", string.Join('\n', registrations)));
    }

    private static string EntityProperties(CrudModel model)
    {
        return string.Join(
            '\n',
            model.RequestFields.Select(field =>
                $"    public {field.Declaration} {field.Name} {{ get; set; }}{(field.RequiresDefault ? " = string.Empty;" : string.Empty)}"));
    }

    private static string RequestProperties(CrudModel model)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < model.RequestFields.Count; index++)
        {
            var field = model.RequestFields[index];

            if (index > 0)
            {
                builder.AppendLine();
            }

            if (field.IsRequired)
            {
                builder.AppendLine("    [Required]");
            }

            if (field.IsString)
            {
                builder.AppendLine($"    [StringLength({field.MaxLength ?? 200})]");
            }

            builder
                .Append("    public ")
                .Append(field.Declaration)
                .Append(' ')
                .Append(field.Name)
                .Append(" { get; set; }");

            if (field.RequiresDefault)
            {
                builder.Append(" = string.Empty;");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }

    private static string RequestUsings(CrudModel model)
    {
        return model.RequestFields.Any(field => field.IsString)
            ? Usings(model.ContractsNamespace, "System.ComponentModel.DataAnnotations")
            : string.Empty;
    }

    private static string ResponseParameters(CrudModel model)
    {
        return string.Join(
            ", ",
            new[] { $"{model.KeyType} {model.KeyName}" }.Concat(
                model.ResponseFields
                    .Where(field => field.Name != model.KeyName)
                    .Select(field => $"{field.Declaration} {field.Name}")));
    }

    private static string ResponseArguments(CrudModel model)
    {
        return string.Join(
            ", ",
            new[] { $"{model.EntityVariable}.{model.KeyName}" }.Concat(
                model.ResponseFields
                    .Where(field => field.Name != model.KeyName)
                    .Select(field => $"{model.EntityVariable}.{field.Name}")));
    }

    private static string Initializers(CrudModel model)
    {
        return string.Join(
            '\n',
            model.RequestFields.Select(field =>
                $"            {field.Name} = request.{field.Name},"));
    }

    private static string Assignments(CrudModel model)
    {
        return string.Join(
            '\n',
            model.RequestFields.Select(field =>
                $"        {model.EntityVariable}.{field.Name} = request.{field.Name};"));
    }

    private static string Usings(string current, params string[] namespaces)
    {
        var lines = namespaces
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != current)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Select(value => $"using {value};")
            .ToArray();

        return lines.Length == 0
            ? string.Empty
            : string.Join('\n', lines) + "\n\n";
    }

    private static string RouteConstraint(string type)
    {
        return type switch
        {
            "int" => "int",
            "long" => "long",
            "Guid" => "guid",
            _ => "string",
        };
    }

    private static string DeleteOperation(CrudModel model)
    {
        return model.IsSoftDeletable
            ? $"""
                       {model.EntityVariable}.IsDeleted = true;
                       {model.EntityVariable}.DeletedAt = DateTimeOffset.UtcNow;
               """
            : $"db.Set<{model.Entity}>().Remove({model.EntityVariable});";
    }

    private static string Render(
        string template,
        params (string Token, string Value)[] replacements)
    {
        var content = template;

        foreach (var (token, value) in replacements)
        {
            content = content.Replace(token, value, StringComparison.Ordinal);
        }

        return content;
    }
}
