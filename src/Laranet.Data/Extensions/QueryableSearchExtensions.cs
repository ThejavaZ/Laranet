using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides search operations for queryable collections.
/// </summary>
public static class QueryableSearchExtensions
{
    /// <summary>
    /// Filters a query by searching for a term inside a string property.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="selector">The selector that identifies the string property to search.</param>
    /// <param name="term">
    /// The term to search for. Null, empty, or whitespace-only values leave the query
    /// unchanged.
    /// </param>
    /// <returns>
    /// A query that contains only items whose selected property is not null and contains
    /// the specified term.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The term is captured through a closure so providers can parameterize it instead
    /// of embedding it as a SQL literal.
    /// </para>
    /// <para>
    /// The comparison is delegated to the provider's translation of
    /// <see cref="string.Contains(string)"/>. Case sensitivity, collation, and wildcard
    /// semantics therefore depend on the provider and the configured database collation.
    /// For example, SQLite and LINQ to Objects perform a case-sensitive ordinal
    /// comparison, while other providers may use a case-insensitive collation.
    /// </para>
    /// <para>
    /// The selector should be a null-safe property access. Items whose selected property
    /// is <see langword="null"/> are always excluded.
    /// </para>
    /// </remarks>
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

        var termHolder = new StrongBox<string>(term);

        var termExpression = Expression.Field(
            Expression.Constant(termHolder),
            nameof(StrongBox<string>.Value)
        );

        var contains = Expression.Call(
            property,
            nameof(string.Contains),
            Type.EmptyTypes,
            termExpression
        );

        var predicate = Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(notNull, contains),
            selector.Parameters
        );

        return source.Where(predicate);
    }
}
