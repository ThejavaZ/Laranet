using System.Text;

namespace Laranet.Cli;

/// <summary>
/// Small console styling helper for Laranet.Cli.
/// </summary>
/// <remarks>
/// Color is used sparingly and only when the standard output is an interactive
/// terminal. Setting <c>NO_COLOR</c> disables color entirely.
/// </remarks>
internal static class CliStyle
{
    private static bool ColorEnabled =>
        !Console.IsOutputRedirected
        && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    /// <summary>
    /// Initializes console output for Unicode tree characters.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Some hosts do not allow changing the output encoding.
        }
    }

    /// <summary>
    /// Writes a success message in green.
    /// </summary>
    public static void WriteSuccess(TextWriter output, string message)
    {
        WriteColored(output, message, ConsoleColor.Green);
    }

    /// <summary>
    /// Writes a warning message in yellow.
    /// </summary>
    public static void WriteWarning(TextWriter output, string message)
    {
        WriteColored(output, message, ConsoleColor.Yellow);
    }

    /// <summary>
    /// Writes an error message in red, followed by the recommended next action.
    /// </summary>
    public static void WriteError(
        TextWriter output,
        string message,
        string? nextStep = null)
    {
        WriteColored(output, $"error: {message}", ConsoleColor.Red);

        if (!string.IsNullOrWhiteSpace(nextStep))
        {
            output.WriteLine($"Next: {nextStep}");
        }
    }

    /// <summary>
    /// Writes a section header in the requested color.
    /// </summary>
    public static void WriteHeader(TextWriter output, string message)
    {
        WriteColored(output, message, ConsoleColor.Cyan);
    }

    /// <summary>
    /// Writes a command name in green.
    /// </summary>
    public static void WriteCommand(TextWriter output, string message)
    {
        if (!ColorEnabled)
        {
            output.Write(message);
            return;
        }

        output.Write($"\u001b[92m{message}\u001b[0m");
    }

    /// <summary>
    /// Writes a suggested next step.
    /// </summary>
    public static void WriteNextStep(TextWriter output, string message)
    {
        WriteColored(output, $"Next: {message}", ConsoleColor.Cyan);
    }

    /// <summary>
    /// Returns the text colored when color is enabled.
    /// </summary>
    public static string Color(string text, ConsoleColor color)
    {
        if (!ColorEnabled)
        {
            return text;
        }

        return $"\u001b[{ColorCode(color)}m{text}\u001b[0m";
    }

    private static void WriteColored(TextWriter output, string message, ConsoleColor color)
    {
        if (!ColorEnabled)
        {
            output.WriteLine(message);
            return;
        }

        output.WriteLine($"\u001b[{ColorCode(color)}m{message}\u001b[0m");
    }

    private static int ColorCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => 30,
            ConsoleColor.DarkRed => 31,
            ConsoleColor.DarkGreen => 32,
            ConsoleColor.DarkYellow => 33,
            ConsoleColor.DarkBlue => 34,
            ConsoleColor.DarkMagenta => 35,
            ConsoleColor.DarkCyan => 36,
            ConsoleColor.Gray => 37,
            ConsoleColor.Red => 91,
            ConsoleColor.Green => 92,
            ConsoleColor.Yellow => 93,
            ConsoleColor.Blue => 94,
            ConsoleColor.Magenta => 95,
            ConsoleColor.Cyan => 96,
            ConsoleColor.White => 97,
            _ => 37,
        };
    }

    /// <summary>
    /// Computes the Levenshtein distance between two strings.
    /// </summary>
    public static int LevenshteinDistance(string left, string right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return right?.Length ?? 0;
        }

        if (string.IsNullOrEmpty(right))
        {
            return left.Length;
        }

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    /// <summary>
    /// Finds the closest known command name.
    /// </summary>
    public static string? FindClosest(string input, IEnumerable<string> candidates)
    {
        var best = (Name: (string?)null, Distance: int.MaxValue);

        foreach (var candidate in candidates)
        {
            var distance = LevenshteinDistance(input, candidate);

            if (distance < best.Distance)
            {
                best = (candidate, distance);
            }
        }

        if (best.Name is null || best.Distance > 3)
        {
            return null;
        }

        return best.Name;
    }
}
