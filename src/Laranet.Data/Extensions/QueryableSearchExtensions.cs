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
    /// <para>
    /// To search across several properties at the same time, use the
    /// <see cref="Search{T}(IQueryable{T}, IEnumerable{Expression{Func{T, string?}}}, string?)"/>
    /// overload.
    /// </para>
    /// </remarks>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> source,
        Expression<Func<T, string?>> selector,
        string? term)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Search([selector], term);
    }

    /// <summary>
    /// Filters a query by searching for a term inside any of several string properties.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="selectors">
    /// The selectors that identify the string properties to search. A match in any
    /// selector includes the item.
    /// </param>
    /// <param name="term">
    /// The term to search for. Null, empty, or whitespace-only values leave the query
    /// unchanged.
    /// </param>
    /// <returns>
    /// A query that contains items for which at least one selected property is not null
    /// and contains the specified term.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="selectors"/>, or any element of
    /// <paramref name="selectors"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="selectors"/> is empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The selectors are combined with a logical <c>OR</c> in a single expression tree,
    /// so the provider receives one translated query. The term is parameterized, and
    /// each selected property is null-checked before <see cref="string.Contains(string)"/>
    /// is evaluated.
    /// </para>
    /// <para>
    /// Matching follows the same provider-dependent comparison rules as the
    /// single-selector overload. Case sensitivity, collation, and wildcard semantics
    /// depend on the provider and the configured database collation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var products = dbContext.Products.Search(
    ///     [product => product.Name, product => product.Description],
    ///     request.Search);
    /// </code>
    /// </example>
    public static IQueryable<T> Search<T>(
        this IQueryable<T> source,
        IEnumerable<Expression<Func<T, string?>>> selectors,
        string? term)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selectors);

        var selectorArray = selectors.ToArray();

        if (selectorArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one selector is required.",
                nameof(selectors));
        }

        foreach (var selector in selectorArray)
        {
            ArgumentNullException.ThrowIfNull(selector);
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return source;
        }

        var parameter = Expression.Parameter(typeof(T), "item");

        var termHolder = new StrongBox<string>(term);

        var termExpression = Expression.Field(
            Expression.Constant(termHolder),
            nameof(StrongBox<string>.Value)
        );

        Expression? predicate = null;

        foreach (var selector in selectorArray)
        {
            var property = new ParameterReplaceVisitor(
                selector.Parameters[0],
                parameter)
                .Visit(selector.Body);

            var notNull = Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(string))
            );

            var contains = Expression.Call(
                property,
                nameof(string.Contains),
                Type.EmptyTypes,
                termExpression
            );

            var match = Expression.AndAlso(notNull, contains);

            predicate = predicate is null
                ? match
                : Expression.OrElse(predicate, match);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);

        return source.Where(lambda);
    }

    private sealed class ParameterReplaceVisitor(
        ParameterExpression from,
        ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }
}
