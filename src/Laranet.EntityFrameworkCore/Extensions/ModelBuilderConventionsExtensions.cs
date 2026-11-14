using System.Linq.Expressions;
using Laranet.EntityFrameworkCore.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Laranet.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides opt-in Entity Framework Core conventions for model configuration.
/// </summary>
public static class ModelBuilderConventionsExtensions
{
    private const string SoftDeleteFilterKey = "LaranetSoftDelete";

    /// <summary>
    /// Applies a native Entity Framework Core query filter that excludes soft-deleted
    /// entities from queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is applied to root entity types that implement
    /// <see cref="ISoftDeletable"/>. Owned types are ignored.
    /// </para>
    /// <para>
    /// Call this method after configuring your own query filters. When the entity type
    /// has an anonymous filter, the soft-delete condition is combined with it using a
    /// logical <c>AND</c>. When it has named filters, a named
    /// <c>LaranetSoftDelete</c> filter is added so Entity Framework Core composes both.
    /// When it has no filters, an anonymous filter is installed.
    /// </para>
    /// <para>
    /// Use <c>IgnoreQueryFilters()</c> on a query to include soft-deleted entities.
    /// </para>
    /// </remarks>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The same model builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="modelBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static ModelBuilder ApplySoftDeleteQueryFilters(
        this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            Expression body = Expression.Not(
                Expression.Property(
                    parameter,
                    nameof(ISoftDeletable.IsDeleted)));

            var declaredFilters = entityType
                .GetDeclaredQueryFilters()
                .ToList();

            var anonymousFilter = declaredFilters
                .FirstOrDefault(filter => filter.IsAnonymous);

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            if (anonymousFilter?.Expression is { } existingExpression)
            {
                body = Expression.AndAlso(
                    ReplaceParameter(
                        existingExpression.Body,
                        existingExpression.Parameters[0],
                        parameter),
                    body);

                entityBuilder.HasQueryFilter(
                    Expression.Lambda(body, parameter));
            }
            else if (declaredFilters.Count > 0)
            {
                entityBuilder.HasQueryFilter(
                    SoftDeleteFilterKey,
                    Expression.Lambda(body, parameter));
            }
            else
            {
                entityBuilder.HasQueryFilter(
                    Expression.Lambda(body, parameter));
            }
        }

        return modelBuilder;
    }

    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression source,
        ParameterExpression target)
    {
        return new ParameterReplacingVisitor(source, target).Visit(body)
            ?? body;
    }

    private sealed class ParameterReplacingVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return ReferenceEquals(node, source)
                ? target
                : base.VisitParameter(node);
        }
    }
}
