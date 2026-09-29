namespace Satisfactory.Planner.Cli.CommandLine;

/// <summary>
/// The usage and help texts of the command line, exactly as Python 3.14 argparse formats them for the planner's
/// arguments with <c>prog="satisfactory-planner"</c> (80-column fallback width, no colours). Every line ends with
/// <c>\n</c>; use the overloads taking a <c>newLine</c> to write them with the platform newline (Python's text-mode
/// console streams translate <c>\n</c> to <see cref="Environment.NewLine"/>).
/// </summary>
public static class UsageText
{
    /// <summary>The program name shown in the usage and error texts.</summary>
    public const string ProgramName = "satisfactory-planner";

    /// <summary>The usage lines (argparse <c>format_usage()</c>), ending with <c>\n</c>.</summary>
    public const string Usage =
        "usage: satisfactory-planner [-h] [--data DATA] [--out OUT]\n" +
        "                            [--miner {mk1,mk2,mk3}]\n" +
        "                            [--purity {impure,normal,pure}] [--include-ingots]\n";

    /// <summary>The full help text (argparse <c>format_help()</c>, printed for <c>-h</c>/<c>--help</c>), ending with <c>\n</c>.</summary>
    public const string Help =
        Usage +
        "\n" +
        "options:\n" +
        "  -h, --help            show this help message and exit\n" +
        "  --data DATA           path to the game data file (downloaded there if\n" +
        "                        missing)\n" +
        "  --out OUT             folder for the output files (created if missing)\n" +
        "  --miner {mk1,mk2,mk3}\n" +
        "  --purity {impure,normal,pure}\n" +
        "  --include-ingots\n";

    /// <summary>
    /// Formats a usage error the way argparse writes it to standard error before exiting with code 2:
    /// the usage lines, then <c>satisfactory-planner: error: {message}</c>.
    /// </summary>
    /// <param name="message">The error message (for example <see cref="CommandLineParseResult.Error"/>).</param>
    /// <returns>The error text, every line ending with <c>\n</c>.</returns>
    public static string FormatError(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Usage + ProgramName + ": error: " + message + "\n";
    }

    /// <summary>Formats a usage error (see <see cref="FormatError(string)"/>) with <c>\n</c> replaced by <paramref name="newLine"/>.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="newLine">The line terminator to write (the CLI passes <see cref="Environment.NewLine"/>).</param>
    /// <returns>The error text with every <c>\n</c> replaced by <paramref name="newLine"/>.</returns>
    public static string FormatError(string message, string newLine)
    {
        ArgumentNullException.ThrowIfNull(newLine);
        return FormatError(message).Replace("\n", newLine, StringComparison.Ordinal);
    }

    /// <summary>Returns <see cref="Help"/> with <c>\n</c> replaced by <paramref name="newLine"/>.</summary>
    /// <param name="newLine">The line terminator to write (the CLI passes <see cref="Environment.NewLine"/>).</param>
    /// <returns>The help text with every <c>\n</c> replaced by <paramref name="newLine"/>.</returns>
    public static string FormatHelp(string newLine)
    {
        ArgumentNullException.ThrowIfNull(newLine);
        return Help.Replace("\n", newLine, StringComparison.Ordinal);
    }
}
