namespace Laranet.Cli.Generation;

/// <summary>
/// Optional settings used by generators that expose additional profiles.
/// </summary>
internal sealed record GenerationSettings(
    MediatorKind Mediator = MediatorKind.None,
    CqrsProfile Profile = CqrsProfile.Simple,
    bool WithHandler = false,
    bool WithTests = false);

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
