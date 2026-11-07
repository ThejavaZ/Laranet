using System.Linq.Expressions;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides search operations for queryable collections.
/// </summary>
public static class QueryableSearchExtensions
{
    /// <summary>
    /// Filters a query by searching for a term inside a string property.
    /// </summary>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> source,
        Expression<Func<T, string?>> selector,
        string? term)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (string.IsNullOrWhiteSpace(term))
        {
            return source;
        }

        var property = selector.Body;

        var notNull = Expression.NotEqual(
            property,
            Expression.Constant(null, typeof(string))
        );

        var contains = Expression.Call(
            property,
            nameof(string.Contains),
            Type.EmptyTypes,
            Expression.Constant(term)
        );

        var predicate = Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(notNull, contains),
            selector.Parameters
        );

        return source.Where(predicate);
    }
}
