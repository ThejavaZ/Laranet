namespace Laranet.Admin;

/// <summary>
/// Identifies an operation that an administrator may perform on a resource.
/// </summary>
public enum AdminOperation
{
    /// <summary>Lists resource rows.</summary>
    List,
    /// <summary>Reads one resource row.</summary>
    Read,
    /// <summary>Creates a resource row.</summary>
    Create,
    /// <summary>Updates a resource row.</summary>
    Update,
    /// <summary>Deletes a resource row.</summary>
    Delete,
}
