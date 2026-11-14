namespace Laranet.EntityFrameworkCore.Conventions;

/// <summary>
/// Marks an entity whose creation and modification timestamps are maintained by the
/// Laranet auditing convention.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the instant when the entity was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the instant when the entity was last modified, or
    /// <see langword="null"/> when it has not been modified since it was created.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }
}
