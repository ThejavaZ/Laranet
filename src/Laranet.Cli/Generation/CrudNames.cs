namespace Laranet.Cli.Generation;

/// <summary>
/// Produces the names used by generated CRUD code.
/// </summary>
internal static class CrudNames
{
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default",
        "delegate", "do", "double", "else", "enum", "event", "explicit",
        "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out",
        "override", "params", "private", "protected", "public", "readonly",
        "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
        "static", "string", "struct", "switch", "this", "throw", "true", "try",
        "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while",
    ];

    /// <summary>
    /// Converts an entity name into a lowercase plural route segment.
    /// </summary>
    /// <param name="name">The normalized entity name.</param>
    /// <returns>The plural route segment.</returns>
    public static string Pluralize(string name)
    {
        var value = name.ToLowerInvariant();

        if (value.EndsWith('y') && value.Length > 1 && !IsVowel(value[^2]))
        {
            return value[..^1] + "ies";
        }

        if (value.EndsWith('s')
            || value.EndsWith('x')
            || value.EndsWith('z')
            || value.EndsWith("ch", StringComparison.Ordinal)
            || value.EndsWith("sh", StringComparison.Ordinal))
        {
            return value + "es";
        }

        return value + "s";
    }

    /// <summary>
    /// Converts an entity name into a PascalCase plural type name.
    /// </summary>
    /// <param name="name">The normalized entity name.</param>
    /// <returns>The plural type name.</returns>
    public static string PluralizeType(string name)
    {
        var plural = Pluralize(name);

        return char.ToUpperInvariant(plural[0]) + plural[1..];
    }

    /// <summary>
    /// Converts a type name into a camelCase local variable name.
    /// </summary>
    /// <param name="name">The normalized type name.</param>
    /// <returns>The local variable name.</returns>
    public static string Variable(string name)
    {
        var value = char.ToLowerInvariant(name[0]) + name[1..];

        return Keywords.Contains(value) ? $"@{value}" : value;
    }

    private static bool IsVowel(char value)
    {
        return value is 'a' or 'e' or 'i' or 'o' or 'u';
    }
}
