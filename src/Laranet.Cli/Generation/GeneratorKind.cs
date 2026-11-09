namespace Laranet.Cli.Generation;

/// <summary>
/// Identifies a code generator.
/// </summary>
internal enum GeneratorKind
{
    Model,
    Service,
    Controller,
    Middleware,
    Resource,
    Repository,
}
