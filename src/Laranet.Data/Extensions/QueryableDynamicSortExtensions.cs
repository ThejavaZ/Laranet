using System.Linq.Expressions;
using System.Reflection;
using Laranet.Data.Sorting;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides dynamic sorting operations for queryable collections.
/// </summary>
public static class QueryableDynamicSortExtensions
{
    /// <summary>
    /// Sorts a query using the name of a public property.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="direction">The sort direction. The default is
    /// <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>An ordered query that can be composed with further sorting operations.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyName"/> is null, empty, whitespace, or does not identify a
    /// public, readable, non-static instance property on <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not a supported <see cref="SortDirection"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Property names are case-sensitive and must match the property exactly.
    /// </para>
    /// <para>
    /// When <paramref name="propertyName"/> originates from external input, use the
    /// overload that accepts an allowlist of permitted property names.
    /// </para>
    /// <para>
    /// The resulting ordering is translated by the underlying provider. Computed or
    /// otherwise unmapped properties may fail at execution time depending on provider
    /// capabilities.
    /// </para>
    /// </remarks>
    public static IOrderedQueryable<T> OrderBy<T>(
        this IQueryable<T> source,
        string propertyName,
        SortDirection direction = SortDirection.Ascending)
    {
        return ApplyOrdering<T>(source, propertyName, direction, false, null);
    }

    /// <summary>
    /// Sorts a query using the name of a public property restricted to an allowlist.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="allowedProperties">
    /// The property names that may be used for sorting. The comparison is ordinal and
    /// case-sensitive. An empty collection rejects every property.
    /// </param>
    /// <param name="direction">The sort direction. The default is
    /// <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>An ordered query that can be composed with further sorting operations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="allowedProperties"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyName"/> is null, empty, whitespace, is not included in
    /// <paramref name="allowedProperties"/>, or does not identify a public, readable,
    /// non-static instance property on <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not a supported <see cref="SortDirection"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when <paramref name="propertyName"/> originates from external
    /// input. The property is validated against the allowlist before the ordering
    /// expression is built.
    /// </para>
    /// <para>
    /// The resulting ordering is translated by the underlying provider. Computed or
    /// otherwise unmapped properties may fail at execution time depending on provider
    /// capabilities.
    /// </para>
    /// </remarks>
    public static IOrderedQueryable<T> OrderBy<T>(
        this IQueryable<T> source,
        string propertyName,
        IReadOnlyCollection<string> allowedProperties,
        SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(allowedProperties);

        return ApplyOrdering<T>(
            source,
            propertyName,
            direction,
            false,
            allowedProperties);
    }

    /// <summary>
    /// Adds a secondary sort order using the name of a public property.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The ordered query to extend.</param>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="direction">The sort direction. The default is
    /// <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>An ordered query that can be composed with further sorting operations.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyName"/> is null, empty, whitespace, or does not identify a
    /// public, readable, non-static instance property on <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not a supported <see cref="SortDirection"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Property names are case-sensitive and must match the property exactly.
    /// </para>
    /// <para>
    /// When <paramref name="propertyName"/> originates from external input, use the
    /// overload that accepts an allowlist of permitted property names.
    /// </para>
    /// <para>
    /// The resulting ordering is translated by the underlying provider. Computed or
    /// otherwise unmapped properties may fail at execution time depending on provider
    /// capabilities.
    /// </para>
    /// </remarks>
    public static IOrderedQueryable<T> ThenBy<T>(
        this IOrderedQueryable<T> source,
        string propertyName,
        SortDirection direction = SortDirection.Ascending)
    {
        return ApplyOrdering<T>(source, propertyName, direction, true, null);
    }

    /// <summary>
    /// Adds a secondary sort order using the name of a public property restricted to an
    /// allowlist.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The ordered query to extend.</param>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="allowedProperties">
    /// The property names that may be used for sorting. The comparison is ordinal and
    /// case-sensitive. An empty collection rejects every property.
    /// </param>
    /// <param name="direction">The sort direction. The default is
    /// <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>An ordered query that can be composed with further sorting operations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="allowedProperties"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="propertyName"/> is null, empty, whitespace, is not included in
    /// <paramref name="allowedProperties"/>, or does not identify a public, readable,
    /// non-static instance property on <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not a supported <see cref="SortDirection"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when <paramref name="propertyName"/> originates from external
    /// input. The property is validated against the allowlist before the ordering
    /// expression is built.
    /// </para>
    /// <para>
    /// The resulting ordering is translated by the underlying provider. Computed or
    /// otherwise unmapped properties may fail at execution time depending on provider
    /// capabilities.
    /// </para>
    /// </remarks>
    public static IOrderedQueryable<T> ThenBy<T>(
        this IOrderedQueryable<T> source,
        string propertyName,
        IReadOnlyCollection<string> allowedProperties,
        SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(allowedProperties);

        return ApplyOrdering<T>(
            source,
            propertyName,
            direction,
            true,
            allowedProperties);
    }

    private static IOrderedQueryable<T> ApplyOrdering<T>(
        IQueryable<T> source,
        string propertyName,
        SortDirection direction,
        bool thenBy,
        IReadOnlyCollection<string>? allowedProperties)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (direction is not SortDirection.Ascending
            and not SortDirection.Descending)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "The specified sort direction is not supported."
            );
        }

        if (allowedProperties is not null
            && !allowedProperties.Contains(propertyName, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Property '{propertyName}' is not allowed for sorting.",
                nameof(propertyName)
            );
        }

        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance
        );

        if (property?.GetMethod is not { IsPublic: true, IsStatic: false }
            || property.GetIndexParameters().Length != 0)
        {
            throw new ArgumentException(
                $"Property '{propertyName}' does not exist or is not publicly readable on type '{typeof(T).Name}'.",
                nameof(propertyName)
            );
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var propertyAccess = Expression.Property(parameter, property);

        var selector = Expression.Lambda(
            propertyAccess,
            parameter
        );

        var methodName = (thenBy, direction) switch
        {
            (false, SortDirection.Ascending) => nameof(Queryable.OrderBy),
            (false, SortDirection.Descending) => nameof(Queryable.OrderByDescending),
            (true, SortDirection.Ascending) => nameof(Queryable.ThenBy),
            (true, SortDirection.Descending) => nameof(Queryable.ThenByDescending),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

        var expression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), property.PropertyType],
            source.Expression,
            Expression.Quote(selector)
        );

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(expression);
    }
}
