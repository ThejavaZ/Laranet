using System.Collections;
using System.Linq.Expressions;
using Laranet.Admin;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Admin.EntityFrameworkCore;

/// <summary>
/// Implements administrative data operations over an application-provided EF Core
/// <see cref="DbContext"/>.
/// </summary>
public sealed class EfCoreAdminDataAdapter : IAdminDataAdapter, IAdminEnhancementAdapter
{
    private readonly DbContext db;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes a new adapter.
    /// </summary>
    /// <param name="db">The application's DbContext.</param>
    /// <param name="timeProvider">The clock used by soft-delete conventions.</param>
    public EfCoreAdminDataAdapter(
        DbContext db,
        TimeProvider? timeProvider = null)
    {
        this.db = db ?? throw new ArgumentNullException(nameof(db));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<AdminPage> ListAsync(
        AdminResource resource,
        AdminQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var normalized = query.Normalize();
        var source = CreateQuery(resource.ModelType);
        source = ApplySearch(source, resource, normalized.Search);
        source = ApplyFilters(source, resource, normalized.Filters);
        source = ApplyOrdering(source, resource, normalized);
        var total = await LongCountAsync(source, cancellationToken);
        source = QueryableMethod(
            "Skip",
            resource.ModelType,
            source,
            Expression.Constant((normalized.Page - 1) * normalized.PageSize));
        source = QueryableMethod(
            "Take",
            resource.ModelType,
            source,
            Expression.Constant(normalized.PageSize));

        // Project only the fields needed for the list view
        source = ApplyProjection(source, resource);
        var rows = await ToListAsync(source, cancellationToken);

        return new AdminPage(
            rows.Cast<object>().Select(row => ToRow(resource, row)).ToArray(),
            normalized.Page,
            normalized.PageSize,
            total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> ReadAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(resource, key, cancellationToken);

        return entity is null ? null : ToRow(resource, entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>> CreateAsync(
        AdminResource resource,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default)
    {
        var entity = Activator.CreateInstance(resource.ModelType)
            ?? throw new InvalidOperationException(
                $"Could not create {resource.ModelType.Name}.");
        ApplyValues(resource, entity, values);
        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return ToRow(resource, entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>> UpdateAsync(
        AdminResource resource,
        object key,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(resource, key, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"No {resource.Name} row exists with key '{key}'.");
        ApplyValues(resource, entity, values);
        await db.SaveChangesAsync(cancellationToken);

        return ToRow(resource, entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(resource, key, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"No {resource.Name} row exists with key '{key}'.");

        if (resource.DeleteValidator is not null)
        {
            var error = await resource.DeleteValidator(entity, cancellationToken);

            if (error is not null)
            {
                throw new InvalidOperationException(error);
            }
        }

        var isDeleted = resource.ModelType.GetProperty("IsDeleted");
        var deletedAt = resource.ModelType.GetProperty("DeletedAt");

        if (isDeleted?.PropertyType == typeof(bool))
        {
            isDeleted.SetValue(entity, true);

            if (deletedAt?.PropertyType == typeof(DateTimeOffset?))
            {
                deletedAt.SetValue(entity, timeProvider.GetUtcNow());
            }
        }
        else
        {
            db.Remove(entity);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminLookupOption>> GetLookupAsync(
        AdminResource resource,
        AdminRelation relation,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var source = CreateQuery(relation.RelatedType);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var parameter = Expression.Parameter(relation.RelatedType, "entity");
            var property = Expression.Property(parameter, relation.DisplayProperty);
            var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(
                property,
                nameof(string.Contains),
                Type.EmptyTypes,
                Expression.Constant(search));
            var clause = Expression.AndAlso(notNull, contains);
            var predicate = Expression.Lambda(clause, parameter);
            source = QueryableMethod("Where", relation.RelatedType, source, predicate);
        }

        var rows = await ToListAsync(source, cancellationToken);

        return rows.Cast<object>()
            .Select(entity =>
            {
                var type = entity.GetType();
                var key = type.GetProperty(relation.KeyProperty)?.GetValue(entity);
                var label = type.GetProperty(relation.DisplayProperty)?.GetValue(entity);

                return new AdminLookupOption(key!, Convert.ToString(label) ?? string.Empty);
            })
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<int> ExecuteBulkActionAsync(
        AdminResource resource,
        string action,
        IReadOnlyList<object> keys,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(action, "soft-delete", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"The EF Core adapter does not support the bulk action '{action}'.");
        }

        var count = 0;

        foreach (var key in keys)
        {
            var entity = await FindAsync(resource, key, cancellationToken);

            if (entity is null)
            {
                continue;
            }

            var isDeleted = resource.ModelType.GetProperty("IsDeleted");
            var deletedAt = resource.ModelType.GetProperty("DeletedAt");

            if (isDeleted?.PropertyType != typeof(bool))
            {
                throw new InvalidOperationException(
                    $"Resource '{resource.Name}' does not support soft-delete.");
            }

            isDeleted.SetValue(entity, true);

            if (deletedAt?.PropertyType == typeof(DateTimeOffset?))
            {
                deletedAt.SetValue(entity, timeProvider.GetUtcNow());
            }

            count++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return count;
    }

    private IQueryable CreateQuery(Type entityType)
    {
        return (IQueryable)(typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType)
            .Invoke(db, null)!);
    }

    private async Task<object?> FindAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken)
    {
        var source = CreateQuery(resource.ModelType);
        var parameter = Expression.Parameter(resource.ModelType, "entity");
        var property = Expression.Property(parameter, resource.KeyField.Name);
        var convertedKey = ConvertValue(key, property.Type);
        var predicate = Expression.Lambda(
            Expression.Equal(property, Expression.Constant(convertedKey, property.Type)),
            parameter);
        source = QueryableMethod("Where", resource.ModelType, source, predicate);
        var rows = await ToListAsync(source, cancellationToken);

        return rows.Cast<object>().SingleOrDefault();
    }

    private IQueryable ApplySearch(
        IQueryable source,
        AdminResource resource,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search) || resource.SearchFields.Count == 0)
        {
            return source;
        }

        var parameter = Expression.Parameter(resource.ModelType, "entity");
        Expression? body = null;

        foreach (var field in resource.SearchFields.Where(field => field.Type == typeof(string)))
        {
            var property = Expression.Property(parameter, field.Name);
            var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(
                property,
                nameof(string.Contains),
                Type.EmptyTypes,
                Expression.Constant(search));
            var clause = Expression.AndAlso(notNull, contains);
            body = body is null ? clause : Expression.OrElse(body, clause);
        }

        return body is null
            ? source
            : QueryableMethod(
                "Where",
                resource.ModelType,
                source,
                Expression.Lambda(body, parameter));
    }

    private IQueryable ApplyFilters(
        IQueryable source,
        AdminResource resource,
        IReadOnlyDictionary<string, string?>? filters)
    {
        if (filters is null)
        {
            return source;
        }

        foreach (var filter in resource.Filters)
        {
            if (!filters.TryGetValue(filter.Name, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var parameter = Expression.Parameter(resource.ModelType, "entity");
            var property = Expression.Property(parameter, filter.Name);
            var converted = ConvertValue(value, property.Type);
            var predicate = Expression.Lambda(
                Expression.Equal(property, Expression.Constant(converted, property.Type)),
                parameter);
            source = QueryableMethod("Where", resource.ModelType, source, predicate);
        }

        return source;
    }

    private IQueryable ApplyOrdering(
        IQueryable source,
        AdminResource resource,
        AdminQuery query)
    {
        var name = query.SortBy ?? resource.DefaultOrderField;

        if (string.IsNullOrWhiteSpace(name)
            || !resource.OrderableFields.Any(field => field.Name == name))
        {
            return source;
        }

        var descending = query.SortBy is null
            ? resource.DefaultOrderDescending
            : query.SortDescending;
        var parameter = Expression.Parameter(resource.ModelType, "entity");
        var property = Expression.Property(parameter, name);
        var selector = Expression.Lambda(property, parameter);
        var method = descending ? "OrderByDescending" : "OrderBy";

        return QueryableMethod(
            method,
            resource.ModelType,
            source,
            [selector],
            property.Type);
    }

    private static IQueryable ApplyProjection(
        IQueryable source,
        AdminResource resource)
    {
        // For now, just return source as-is since we need all fields for ToRow
        // Column projection can be added later when we have a DTO type
        return source;
    }

    private static IReadOnlyDictionary<string, object?> ToRow(
        AdminResource resource,
        object entity)
    {
        return resource.Fields.ToDictionary(
            field => field.Name,
            field => entity.GetType().GetPropertyValue(entity, field.Name));
    }

    private static void ApplyValues(
        AdminResource resource,
        object entity,
        IReadOnlyDictionary<string, object?> values)
    {
        foreach (var field in resource.Form?.Fields ?? resource.Fields)
        {
            if (field.IsKey || field.IsReadOnly)
            {
                continue;
            }

            if (!values.TryGetValue(field.Name, out var value))
            {
                continue;
            }

            var property = entity.GetType().GetProperty(field.Name);

            if (property?.CanWrite == true)
            {
                property.SetValue(entity, ConvertValue(value, property.PropertyType));
            }
        }
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var nullable = Nullable.GetUnderlyingType(targetType);
        var type = nullable ?? targetType;

        if (value.GetType() == type)
        {
            return value;
        }

        var stringValue = value.ToString();

        if (string.IsNullOrWhiteSpace(stringValue))
        {
            if (nullable is not null)
            {
                return null;
            }

            throw new FormatException(
                $"A value is required for type '{type.Name}'.");
        }

        if (type == typeof(Guid))
        {
            if (!Guid.TryParse(stringValue, out var guid))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid GUID.");
            }

            return guid;
        }

        if (type == typeof(int))
        {
            if (!int.TryParse(stringValue, out var intValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid integer.");
            }

            return intValue;
        }

        if (type == typeof(long))
        {
            if (!long.TryParse(stringValue, out var longValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid integer.");
            }

            return longValue;
        }

        if (type == typeof(decimal))
        {
            if (!decimal.TryParse(
                    stringValue,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var decimalValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid decimal number.");
            }

            return decimalValue;
        }

        if (type == typeof(double))
        {
            if (!double.TryParse(
                    stringValue,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var doubleValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid number.");
            }

            return doubleValue;
        }

        if (type == typeof(float))
        {
            if (!float.TryParse(
                    stringValue,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var floatValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid number.");
            }

            return floatValue;
        }

        if (type == typeof(DateTime))
        {
            if (!DateTime.TryParse(stringValue, out var dateTimeValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid date.");
            }

            return dateTimeValue;
        }

        if (type == typeof(DateTimeOffset))
        {
            if (!DateTimeOffset.TryParse(stringValue, out var dateTimeOffsetValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid date.");
            }

            return dateTimeOffsetValue;
        }

        if (type == typeof(DateOnly))
        {
            if (!DateOnly.TryParse(stringValue, out var dateOnlyValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid date.");
            }

            return dateOnlyValue;
        }

        if (type == typeof(TimeOnly))
        {
            if (!TimeOnly.TryParse(stringValue, out var timeOnlyValue))
            {
                throw new FormatException(
                    $"'{stringValue}' is not a valid time.");
            }

            return timeOnlyValue;
        }

        if (type == typeof(bool))
        {
            if (bool.TryParse(stringValue, out var boolValue))
            {
                return boolValue;
            }

            // Handle "on" from checkbox
            if (string.Equals(stringValue, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            throw new FormatException(
                $"'{stringValue}' is not a valid boolean.");
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, stringValue, ignoreCase: true);
        }

        return Convert.ChangeType(value, type);
    }

    private static IQueryable QueryableMethod(
        string name,
        Type entityType,
        IQueryable source,
        params Expression[] arguments)
    {
        return QueryableMethod(name, entityType, source, arguments, null);
    }

    private static IQueryable QueryableMethod(
        string name,
        Type entityType,
        IQueryable source,
        IReadOnlyList<Expression> arguments,
        Type? secondGenericType = null)
    {
        var method = typeof(Queryable)
            .GetMethods()
            .Single(method =>
                method.Name == name
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == arguments.Count + 1
                && method.GetParameters().Skip(1).Zip(arguments).All(pair =>
                    IsExpressionParameter(pair.First.ParameterType, pair.Second)
                    || pair.First.ParameterType == typeof(int)));
        var genericArguments = method.GetGenericArguments().Length == 1
            ? [entityType]
            : new[] { entityType, secondGenericType! };
        var generic = method.MakeGenericMethod(genericArguments);
        var values = new object[] { source }
            .Concat(arguments.Select(argument =>
                argument is ConstantExpression constant
                    ? constant.Value!
                    : (object)argument))
            .ToArray();

        return (IQueryable)generic.Invoke(null, values)!;
    }

    private static bool IsExpressionParameter(Type type, Expression argument)
    {
        if (!type.IsGenericType
            || type.GetGenericTypeDefinition() != typeof(Expression<>)
            || argument is not LambdaExpression lambda)
        {
            return false;
        }

        var delegateType = type.GetGenericArguments()[0];

        return delegateType.IsGenericType
            && delegateType.GetGenericArguments().Length == lambda.Parameters.Count + 1;
    }

    private static async Task<long> LongCountAsync(
        IQueryable source,
        CancellationToken cancellationToken)
    {
        var method = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(EntityFrameworkQueryableExtensions.LongCountAsync)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 2);
        var task = (Task)method
            .MakeGenericMethod(source.ElementType)
            .Invoke(null, [source, cancellationToken])!;
        await task;

        return (long)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static async Task<IList> ToListAsync(
        IQueryable source,
        CancellationToken cancellationToken)
    {
        var method = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 2);
        var task = (Task)method
            .MakeGenericMethod(source.ElementType)
            .Invoke(null, [source, cancellationToken])!;
        await task;

        return (IList)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }
}

internal static class AdminReflectionExtensions
{
    public static object? GetPropertyValue(this Type type, object instance, string name)
    {
        return type.GetProperty(name)?.GetValue(instance);
    }
}
