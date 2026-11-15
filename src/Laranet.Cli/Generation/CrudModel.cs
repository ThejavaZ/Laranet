namespace Laranet.Cli.Generation;

/// <summary>
/// Describes the files and names used by one CRUD generation.
/// </summary>
/// <param name="Entity">The entity type name.</param>
/// <param name="EntityVariable">The camelCase variable name.</param>
/// <param name="Controller">The controller type name.</param>
/// <param name="Service">The service type name.</param>
/// <param name="Repository">The repository type name.</param>
/// <param name="RepositoryInterface">The repository interface name.</param>
/// <param name="Request">The request DTO name.</param>
/// <param name="Response">The response DTO name.</param>
/// <param name="Route">The lowercase plural route segment.</param>
/// <param name="DbContextType">The fully qualified context type.</param>
/// <param name="EntityDirectory">The entity output directory.</param>
/// <param name="EntityNamespace">The entity namespace.</param>
/// <param name="ContractsDirectory">The DTO output directory.</param>
/// <param name="ContractsNamespace">The DTO namespace.</param>
/// <param name="ServiceDirectory">The service output directory.</param>
/// <param name="ServiceNamespace">The service namespace.</param>
/// <param name="RepositoryDirectory">The repository implementation directory.</param>
/// <param name="RepositoryNamespace">The repository implementation namespace.</param>
/// <param name="RepositoryInterfaceDirectory">The repository interface directory.</param>
/// <param name="RepositoryInterfaceNamespace">The repository interface namespace.</param>
/// <param name="ControllerDirectory">The controller output directory.</param>
/// <param name="ControllerNamespace">The controller namespace.</param>
/// <param name="ExtensionsDirectory">The registration extension directory.</param>
/// <param name="ExtensionsNamespace">The registration extension namespace.</param>
/// <param name="Fields">The mutable fields of the entity.</param>
internal sealed record CrudModel(
    string Entity,
    string EntityVariable,
    string Controller,
    string Service,
    string Repository,
    string RepositoryInterface,
    string Request,
    string Response,
    string Route,
    string DbContextType,
    string EntityDirectory,
    string EntityNamespace,
    string ContractsDirectory,
    string ContractsNamespace,
    string ServiceDirectory,
    string ServiceNamespace,
    string RepositoryDirectory,
    string RepositoryNamespace,
    string RepositoryInterfaceDirectory,
    string RepositoryInterfaceNamespace,
    string ControllerDirectory,
    string ControllerNamespace,
    string ExtensionsDirectory,
    string ExtensionsNamespace,
    IReadOnlyList<CrudField> Fields)
{
    /// <summary>
    /// Gets the metadata read from an existing source entity, if any.
    /// </summary>
    public EntityMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether the CRUD surface targets an existing entity.
    /// </summary>
    public bool IsExistingEntity => Metadata is not null;

    /// <summary>
    /// Gets the key type used by routes and DTOs.
    /// </summary>
    public string KeyType => Metadata?.KeyType ?? "int";

    /// <summary>
    /// Gets the key property name.
    /// </summary>
    public string KeyName => Metadata?.KeyName ?? "Id";

    /// <summary>
    /// Gets the request fields.
    /// </summary>
    public IReadOnlyList<CrudField> RequestFields => Metadata?.RequestFields ?? Fields;

    /// <summary>
    /// Gets the response fields.
    /// </summary>
    public IReadOnlyList<CrudField> ResponseFields => Metadata?.ResponseFields
        ?? [new CrudField("Id", "int", false), .. Fields];

    /// <summary>
    /// Gets a value indicating whether delete should use soft-delete semantics.
    /// </summary>
    public bool IsSoftDeletable => Metadata?.IsSoftDeletable == true;

    /// <summary>
    /// Gets the context type with an absolute namespace qualifier.
    /// </summary>
    public string DbContextReference => $"global::{DbContextType}";
}
