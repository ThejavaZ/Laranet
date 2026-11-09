using Laranet.Cli.Generation;

namespace Laranet.Cli;

/// <summary>
/// Prompts the user for values when a command runs in interactive mode.
/// </summary>
internal sealed class ConsoleInteraction(TextReader input, TextWriter output)
{
    /// <summary>
    /// Prompts until the user provides a value accepted by <paramref name="parse"/>.
    /// </summary>
    /// <typeparam name="T">The parsed value type.</typeparam>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="parse">Validates and converts the entered value.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="CliException">The input ended before a value was provided.</exception>
    public T AskUntil<T>(string prompt, Func<string, T> parse)
    {
        while (true)
        {
            var value = Ask(prompt);

            if (value is null)
            {
                throw new CliException("Interactive input ended unexpectedly.");
            }

            try
            {
                return parse(value);
            }
            catch (CliException exception)
            {
                output.WriteLine($"error: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Prompts for a template, using <c>webapi</c> when the user accepts the default.
    /// </summary>
    /// <returns>The selected template.</returns>
    public ProjectTemplate AskTemplate()
    {
        return AskUntil(
            "Template [webapi/console] (webapi): ",
            value => ProjectTemplates.Parse(
                string.IsNullOrWhiteSpace(value) ? "webapi" : value));
    }

    /// <summary>
    /// Prompts for a project layout, using <c>project</c> when the user accepts the default.
    /// </summary>
    /// <returns>The selected layout.</returns>
    public ProjectLayout AskLayout()
    {
        return AskUntil(
            "Layout [project/repository] (project): ",
            value => ProjectLayoutParser.Parse(
                string.IsNullOrWhiteSpace(value) ? "project" : value));
    }

    /// <summary>
    /// Asks a yes/no question. Only <c>y</c> and <c>yes</c> are accepted.
    /// </summary>
    /// <param name="prompt">The prompt text.</param>
    /// <returns><see langword="true"/> when the user confirms.</returns>
    public bool Confirm(string prompt)
    {
        var value = Ask(prompt);

        return value is not null
            && (value.Equals("y", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    private string? Ask(string prompt)
    {
        output.Write(prompt);
        output.Flush();

        return input.ReadLine()?.Trim();
    }
}
