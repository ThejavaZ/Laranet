using Laranet.EntityFrameworkCore.Conventions;

namespace Laranet.Tests.Integration.Data.TestSupport;

internal sealed class AuditedTestRecord : IAuditable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class SoftDeleteTestRecord : ISoftDeletable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

internal sealed class AuditedSoftDeleteTestRecord : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

internal sealed class FilteredSoftDeleteTestRecord : ISoftDeletable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

internal sealed class NamedFilteredSoftDeleteTestRecord : ISoftDeletable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
