namespace Laranet.Cli.Generation;

/// <summary>
/// Optional settings used by generators that expose additional profiles.
/// </summary>
internal sealed record GenerationSettings(
    MediatorKind Mediator = MediatorKind.None,
    CqrsProfile Profile = CqrsProfile.Simple,
    bool WithHandler = false,
    bool WithTests = false,
    DatabaseProvider Provider = DatabaseProvider.SqlServer,
    IReadOnlyList<string>? Entities = null,
    IReadOnlyList<string>? Sections = null);

/// <summary>
/// Identifies the external mediator contract emitted by a generator.
/// </summary>
internal enum MediatorKind
{
    None,
    MediatR,
    Wolverine,
}

/// <summary>
/// Identifies the organization profile used by CQRS generators.
/// </summary>
internal enum CqrsProfile
{
    Simple,
    Standard,
    Clean,
}

/// <summary>
/// Identifies the provider-specific API emitted by the DbContext generator.
/// </summary>
internal enum DatabaseProvider
{
    SqlServer,
    Postgres,
    MySql,
    Sqlite,
}
