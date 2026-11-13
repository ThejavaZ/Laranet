namespace Laranet.Cli.Generation;

/// <summary>
/// Describes one property generated for a CRUD entity and its request DTO.
/// </summary>
/// <param name="Name">The normalized property name.</param>
/// <param name="Type">The C# type without a nullable suffix.</param>
/// <param name="IsNullable">Whether the property accepts <see langword="null"/>.</param>
internal sealed record CrudField(string Name, string Type, bool IsNullable)
{
    /// <summary>
    /// Gets the type declaration, including the nullable suffix.
    /// </summary>
    public string Declaration => IsNullable ? $"{Type}?" : Type;

    /// <summary>
    /// Gets a value indicating whether the property needs a non-null default value.
    /// </summary>
    public bool RequiresDefault => Type == "string" && !IsNullable;

    /// <summary>
    /// Gets a value indicating whether the property is a string.
    /// </summary>
    public bool IsString => Type == "string";
}

/// <summary>
/// Parses the field list used by <c>make:crud</c>.
/// </summary>
internal static class CrudFieldParser
{
    private static readonly IReadOnlyDictionary<string, string> Types =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["string"] = "string",
            ["int"] = "int",
            ["int32"] = "int",
            ["long"] = "long",
            ["int64"] = "long",
            ["decimal"] = "decimal",
            ["double"] = "double",
            ["float"] = "float",
            ["single"] = "float",
            ["bool"] = "bool",
            ["boolean"] = "bool",
            ["datetime"] = "DateTime",
            ["datetimeoffset"] = "DateTimeOffset",
            ["dateonly"] = "DateOnly",
            ["timeonly"] = "TimeOnly",
            ["guid"] = "Guid",
        };

    /// <summary>
    /// Parses a comma-separated list of <c>Name:type</c> fields.
    /// </summary>
    /// <param name="value">The field list, or <see langword="null"/> for the default field.</param>
    /// <returns>The parsed fields.</returns>
    /// <exception cref="CliException">The field list is invalid.</exception>
    public static IReadOnlyList<CrudField> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [new CrudField("Name", "string", IsNullable: false)];
        }

        var fields = new List<CrudField>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in value.Split(',', StringSplitOptions.TrimEntries))
        {
            if (item.Length == 0)
            {
                throw new CliException("Field list contains an empty entry.");
            }

            var separator = item.IndexOf(':', StringComparison.Ordinal);

            if (separator <= 0 || separator == item.Length - 1)
            {
                throw new CliException(
                    $"Field '{item}' must use the 'Name:type' format, such as 'Name:string'.");
            }

            var name = NameNormalizer.Normalize(item[..separator]);

            if (name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                throw new CliException(
                    "'Id' is generated automatically and cannot be declared as a field.");
            }

            if (!names.Add(name))
            {
                throw new CliException($"Field '{name}' is declared more than once.");
            }

            var typeValue = item[(separator + 1)..].Trim();
            var isNullable = typeValue.EndsWith('?');

            if (isNullable)
            {
                typeValue = typeValue[..^1].Trim();
            }

            if (!Types.TryGetValue(typeValue, out var type))
            {
                throw new CliException(
                    $"'{typeValue}' is not a supported field type. Use {string.Join(", ", SupportedTypes())} with an optional '?' suffix.");
            }

            fields.Add(new CrudField(name, type, isNullable));
        }

        return fields;
    }

    private static IEnumerable<string> SupportedTypes()
    {
        return Types.Values.Distinct(StringComparer.Ordinal);
    }
}
