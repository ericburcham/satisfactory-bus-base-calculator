using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Cli.CommandLine;

/// <summary>
/// The outcome of parsing the command line: exactly one of <see cref="Options"/> (success),
/// <see cref="HelpRequested"/> (<c>-h</c>/<c>--help</c>) or <see cref="Error"/> (a usage error) is set.
/// </summary>
/// <param name="Options">The parsed options, or <see langword="null"/> when help was requested or parsing failed.</param>
/// <param name="HelpRequested">
/// <see langword="true"/> when <c>-h</c>/<c>--help</c> was seen; the program then prints <see cref="UsageText.Help"/>
/// to standard output and exits with code 0 (argparse stops parsing at that point).
/// </param>
/// <param name="Error">
/// The argparse-style error message (without the usage lines and the <c>satisfactory-planner: error: </c> prefix; see
/// <see cref="UsageText.FormatError(string)"/>), or <see langword="null"/> when there was no error.
/// </param>
public sealed record CommandLineParseResult(PlannerOptions? Options, bool HelpRequested, string? Error);
