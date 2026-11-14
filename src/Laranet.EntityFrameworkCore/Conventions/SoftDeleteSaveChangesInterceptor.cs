using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Laranet.EntityFrameworkCore.Conventions;

/// <summary>
/// Converts tracked deletions of entities that implement <see cref="ISoftDeletable"/>
/// into updates of the soft-delete flags.
/// </summary>
internal sealed class SoftDeleteSaveChangesInterceptor(
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context, timeProvider.GetUtcNow());
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context, timeProvider.GetUtcNow());
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context, DateTimeOffset now)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;

            if (entry.Entity is IAuditable auditable)
            {
                auditable.UpdatedAt = now;
            }

            entry.State = EntityState.Modified;
        }
    }
}
