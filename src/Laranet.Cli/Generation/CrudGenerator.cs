namespace Laranet.Cli.Generation;

/// <summary>
/// Generates a CRUD surface for an entity.
/// </summary>
internal sealed class CrudGenerator : IGenerator
{
    private readonly CrudProfile profile;
    private readonly IReadOnlyList<CrudField> fields;
    private readonly string dbContextType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrudGenerator"/> class.
    /// </summary>
    /// <param name="profile">The generation profile.</param>
    /// <param name="fields">The mutable entity fields.</param>
    /// <param name="dbContextType">The fully qualified context type.</param>
    public CrudGenerator(
        CrudProfile profile,
        IReadOnlyList<CrudField> fields,
        string dbContextType)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbContextType);

        this.profile = profile;
        this.fields = fields;
        this.dbContextType = dbContextType;
    }

    /// <inheritdoc />
    public GenerationPlan Create(GenerationContext context)
    {
        var model = CreateModel(context);
        var files = new List<GeneratedFile>();

        Add(files, model.EntityDirectory, $"{model.Entity}.cs", CrudTemplates.Entity(model));
        Add(files, model.ContractsDirectory, $"{model.Request}.cs", CrudTemplates.Request(model));
        Add(files, model.ContractsDirectory, $"{model.Response}.cs", CrudTemplates.Response(model));

        switch (profile)
        {
            case CrudProfile.Simple:
                Add(
                    files,
                    model.ControllerDirectory,
                    $"{model.Controller}.cs",
                    CrudTemplates.SimpleController(model));
                break;

            case CrudProfile.Standard:
            case CrudProfile.Modular:
                Add(
                    files,
                    model.ServiceDirectory,
                    $"{model.Service}.cs",
                    CrudTemplates.Service(model));
                Add(
                    files,
                    model.ControllerDirectory,
                    $"{model.Controller}.cs",
                    CrudTemplates.ServiceController(model));
                Add(
                    files,
                    model.ExtensionsDirectory,
                    $"{model.Entity}ServiceCollectionExtensions.cs",
                    CrudTemplates.Registration(model, [
                        $"        services.AddScoped<{model.Service}>();",
                    ]));
                break;

            case CrudProfile.Clean:
                Add(
                    files,
                    model.RepositoryInterfaceDirectory,
                    $"{model.RepositoryInterface}.cs",
                    CrudTemplates.RepositoryInterface(model));
                Add(
                    files,
                    model.RepositoryDirectory,
                    $"{model.Repository}.cs",
                    CrudTemplates.Repository(model));
                Add(
                    files,
                    model.ServiceDirectory,
                    $"{model.Service}.cs",
                    CrudTemplates.CleanService(model));
                Add(
                    files,
                    model.ControllerDirectory,
                    $"{model.Controller}.cs",
                    CrudTemplates.ServiceController(model));
                Add(
                    files,
                    model.ExtensionsDirectory,
                    $"{model.Entity}ServiceCollectionExtensions.cs",
                    CrudTemplates.Registration(model, [
                        $"        services.AddScoped<{model.RepositoryInterface}, {model.Repository}>();",
                        $"        services.AddScoped<{model.Service}>();",
                    ]));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(profile), profile, null);
        }

        return new GenerationPlan(context.Name, files);
    }

    private CrudModel CreateModel(GenerationContext context)
    {
        var entity = context.Name;
        var request = $"{entity}Request";
        var response = $"{entity}Response";
        var controller = $"{entity}Controller";
        var service = $"{entity}Service";
        var repository = $"{entity}Repository";
        var repositoryInterface = $"I{repository}";
        var entityVariable = CrudNames.Variable(entity);
        var route = CrudNames.Pluralize(entity);
        var featureFolder = CrudNames.PluralizeType(entity);

        var targets = profile switch
        {
            CrudProfile.Simple => new CrudTargets(
                Resolve(context, "Models"),
                Resolve(context, "Contracts"),
                Resolve(context, "Contracts"),
                Resolve(context, "Contracts"),
                Resolve(context, "Contracts"),
                Resolve(context, "Controllers"),
                Resolve(context, "Extensions")),
            CrudProfile.Standard => new CrudTargets(
                Resolve(context, "Models"),
                Resolve(context, "Contracts"),
                Resolve(context, "Services"),
                Resolve(context, "Services"),
                Resolve(context, "Services"),
                Resolve(context, "Controllers"),
                Resolve(context, "Extensions")),
            CrudProfile.Clean => new CrudTargets(
                Resolve(context, "Domain", "Entities"),
                Resolve(context, "Application", "Products"),
                Resolve(context, "Application", "Products"),
                Resolve(context, "Infrastructure", "Persistence"),
                Resolve(context, "Application", "Abstractions"),
                Resolve(context, "Controllers"),
                Resolve(context, "Extensions")),
            CrudProfile.Modular => new CrudTargets(
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder),
                Resolve(context, "Features", featureFolder)),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

        return new CrudModel(
            entity,
            entityVariable,
            controller,
            service,
            repository,
            repositoryInterface,
            request,
            response,
            route,
            dbContextType,
            targets.Entity.Directory,
            targets.Entity.Namespace,
            targets.Contracts.Directory,
            targets.Contracts.Namespace,
            targets.Service.Directory,
            targets.Service.Namespace,
            targets.Repository.Directory,
            targets.Repository.Namespace,
            targets.RepositoryInterface.Directory,
            targets.RepositoryInterface.Namespace,
            targets.Controller.Directory,
            targets.Controller.Namespace,
            targets.Extensions.Directory,
            targets.Extensions.Namespace,
            fields);
    }

    private sealed record CrudTargets(
        (string Directory, string Namespace) Entity,
        (string Directory, string Namespace) Contracts,
        (string Directory, string Namespace) Service,
        (string Directory, string Namespace) Repository,
        (string Directory, string Namespace) RepositoryInterface,
        (string Directory, string Namespace) Controller,
        (string Directory, string Namespace) Extensions);

    private static (string Directory, string Namespace) Resolve(
        GenerationContext context,
        params string[] folders)
    {
        return GeneratorSupport.ResolveTarget(context, folders);
    }

    private static void Add(
        List<GeneratedFile> files,
        string directory,
        string fileName,
        string content)
    {
        files.Add(new GeneratedFile(Path.Combine(directory, fileName), content));
    }
}
