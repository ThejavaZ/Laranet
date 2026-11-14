namespace Laranet.EntityFrameworkCore.Conventions;

/// <summary>
/// Marks an entity that is soft-deleted instead of physically removed.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the instant when the entity was soft-deleted, or
    /// <see langword="null"/> when it is active.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }
}
