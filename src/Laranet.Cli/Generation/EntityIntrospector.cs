using System.Text.RegularExpressions;

namespace Laranet.Cli.Generation;

/// <summary>
/// Reads the source shape of an existing entity for CRUD generation.
/// </summary>
internal static partial class EntityIntrospector
{
    public static EntityMetadata? TryRead(string outputDirectory, string entityName)
    {
        var files = Directory
            .EnumerateFiles(outputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .Where(path => string.Equals(
                Path.GetFileNameWithoutExtension(path),
                entityName,
                StringComparison.Ordinal))
            .OrderBy(path => IsModelPath(path) ? 0 : 1)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            return null;
        }

        var definitions = new Dictionary<string, SourceEntity>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(
                     outputDirectory,
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            var definition = Parse(source, file);

            if (definition is not null)
            {
                definitions[definition.Name] = definition;
            }
        }

        var current = definitions.GetValueOrDefault(entityName);

        if (current is null)
        {
            current = Parse(File.ReadAllText(files[0]), files[0]);
        }

        if (current is null)
        {
            return null;
        }

        var chain = new List<SourceEntity>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (current is not null && visited.Add(current.Name))
        {
            chain.Add(current);
            current = current.BaseTypes
                .Select(definitions.GetValueOrDefault)
                .FirstOrDefault(definition => definition is not null);
        }

        var properties = chain
            .SelectMany(item => item.Properties)
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var key = properties.FirstOrDefault(property => property.IsKey)
            ?? properties.FirstOrDefault(property =>
                string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase));

        if (key is null)
        {
            throw new CliException(
                $"The existing entity '{entityName}' does not expose a key property. " +
                "Add an Id property or [Key] before using make:crud.");
        }

        var interfaces = chain.SelectMany(item => item.Interfaces).ToHashSet(
            StringComparer.Ordinal);
        var requestProperties = properties
            .Where(property => !property.IsServerManaged)
            .Select(ToCrudField)
            .ToArray();
        var responseProperties = properties
            .Where(property => !property.IsSoftDeleteState)
            .Select(ToCrudField)
            .ToArray();

        return new EntityMetadata(
            entityName,
            chain[0].Namespace,
            key.Type,
            key.Name,
            responseProperties,
            requestProperties,
            interfaces.Contains("ISoftDeletable"),
            interfaces.Contains("IAuditable"),
            properties.Any(property =>
                string.Equals(property.Name, "CompanyId", StringComparison.Ordinal)));
    }

    private static SourceEntity? Parse(string source, string path)
    {
        var namespaceMatch = NamespaceRegex().Match(source);
        var typeMatch = TypeRegex().Match(source);

        if (!typeMatch.Success)
        {
            return null;
        }

        var baseTypes = (typeMatch.Groups["bases"].Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split('.').Last())
            .ToArray();
        var interfaces = baseTypes
            .Where(value => value.StartsWith('I'))
            .ToArray();
        var attributes = AttributesBefore(source, typeMatch.Index);
        var tableName = TableRegex().Match(attributes).Groups["name"].Value;
        var properties = PropertyRegex()
            .Matches(source)
            .Select(match =>
            {
                var propertyAttributes = match.Groups["attributes"].Value;
                var type = match.Groups["type"].Value;
                var name = match.Groups["name"].Value;
                var isNullable = type.EndsWith('?')
                    || propertyAttributes.Contains("AllowNull", StringComparison.Ordinal);
                var baseType = type.TrimEnd('?');
                var isKey = name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                    || propertyAttributes.Contains("[Key", StringComparison.Ordinal);
                var isSoftDeleteState = name is "IsDeleted" or "DeletedAt";
                var isServerManaged = isKey
                    || name is "CreatedAt" or "UpdatedAt"
                    || isSoftDeleteState;
                var maxLength = MaxLengthRegex().Match(propertyAttributes).Groups["length"].Value;

                return new SourceProperty(
                    name,
                    baseType,
                    isNullable,
                    isKey,
                    isSoftDeleteState,
                    maxLength.Length > 0 ? int.Parse(maxLength) : null);
            })
            .ToArray();

        return new SourceEntity(
            typeMatch.Groups["name"].Value,
            namespaceMatch.Groups["name"].Value,
            baseTypes.Where(value => !interfaces.Contains(value)).ToArray(),
            interfaces,
            properties,
            tableName.Length == 0 ? null : tableName,
            path);
    }

    private static CrudField ToCrudField(SourceProperty property)
    {
        return new CrudField(
            property.Name,
            property.Type,
            property.IsNullable,
            property.MaxLength);
    }

    private static string AttributesBefore(string source, int index)
    {
        var start = source.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
        var previous = source[..start];
        var sameLine = source[start..index];
        var lines = previous
            .Split('\n')
            .Reverse()
            .TakeWhile(line =>
            {
                var trimmed = line.TrimStart();

                return trimmed.StartsWith('[')
                    && !trimmed.Contains(" public ", StringComparison.Ordinal)
                    && !trimmed.Contains(" protected ", StringComparison.Ordinal)
                    && !trimmed.Contains(" internal ", StringComparison.Ordinal)
                    && !trimmed.Contains(" private ", StringComparison.Ordinal);
            })
            .Reverse();

        return sameLine + string.Join('\n', lines);
    }

    private static bool IsModelPath(string path)
    {
        return path.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "Models" or "Entities");
    }

    [GeneratedRegex(@"namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(
        @"(?:class|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*:\s*(?<bases>[^{]+))?")]
    private static partial Regex TypeRegex();

    [GeneratedRegex(
        @"(?<attributes>(?:\[[^\]]+\]\s*)*)(?:public|protected|internal|private)\s+(?:virtual\s+|override\s+|required\s+|static\s+)*(?!(?:class|record|interface|struct|enum)\b)(?<type>[A-Za-z_][A-Za-z0-9_.]*(?:\?)?(?:<[^>]+>)?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{")]
    private static partial Regex PropertyRegex();

    [GeneratedRegex(@"\[Table\s*\(\s*""(?<name>[^""]+)""")]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"\[(?:MaxLength|StringLength)\s*\(\s*(?<length>\d+)")]
    private static partial Regex MaxLengthRegex();

    private sealed record SourceEntity(
        string Name,
        string Namespace,
        IReadOnlyList<string> BaseTypes,
        IReadOnlyList<string> Interfaces,
        IReadOnlyList<SourceProperty> Properties,
        string? TableName,
        string Path);

    private sealed record SourceProperty(
        string Name,
        string Type,
        bool IsNullable,
        bool IsKey,
        bool IsSoftDeleteState,
        int? MaxLength)
    {
        public bool IsServerManaged => IsKey
            || Name is "CreatedAt" or "UpdatedAt"
            || IsSoftDeleteState;
    }
}

internal sealed record EntityMetadata(
    string Name,
    string Namespace,
    string KeyType,
    string KeyName,
    IReadOnlyList<CrudField> ResponseFields,
    IReadOnlyList<CrudField> RequestFields,
    bool IsSoftDeletable,
    bool IsAuditable,
    bool HasCompanyId);
