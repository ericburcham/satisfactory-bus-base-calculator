using System.Globalization;
using System.Text;
using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Cli.CommandLine;

/// <summary>
/// Parses the planner's command line exactly like the Python script's argparse parser (Python 3.14 semantics):
/// <c>--data DATA</c>, <c>--out OUT</c>, <c>--miner {mk1,mk2,mk3}</c>, <c>--purity {impure,normal,pure}</c>,
/// <c>--include-ingots</c> and <c>-h</c>/<c>--help</c>.
/// </summary>
/// <remarks>
/// A port of argparse's <c>_parse_known_args</c> for this fixed option set (no positionals): values as
/// <c>--opt value</c> or <c>--opt=value</c>; unique-prefix abbreviations of long options (<c>--inc</c>); a repeated
/// option overrides the earlier one; a following argument that looks like an option (<c>-x</c>, <c>--out</c>, <c>--</c>)
/// is not taken as a value ("expected one argument"), while negative-number-like strings (<c>-1</c>) and strings
/// containing a space are; <c>-h</c> stops parsing as soon as it is reached, so errors of earlier arguments win but
/// unrecognized arguments do not; unrecognized arguments are reported together after everything else was parsed.
/// </remarks>
public sealed class CommandLineOptionsParser
{
    private const char PrefixChar = '-';

    private readonly List<KeyValuePair<string, OptionAction>> _optionStrings;
    private readonly Dictionary<string, OptionAction> _optionStringActions;

    /// <summary>Initializes a new instance of the <see cref="CommandLineOptionsParser"/> class.</summary>
    public CommandLineOptionsParser()
    {
        var help = new OptionAction(OptionKind.Help, "-h/--help", ArgumentCount: 0, Choices: null);
        var data = new OptionAction(OptionKind.Data, "--data", ArgumentCount: 1, Choices: null);
        var output = new OptionAction(OptionKind.Output, "--out", ArgumentCount: 1, Choices: null);
        var miner = new OptionAction(OptionKind.Miner, "--miner", ArgumentCount: 1, Choices: ["mk1", "mk2", "mk3"]);
        var purity = new OptionAction(
            OptionKind.Purity, "--purity", ArgumentCount: 1, Choices: ["impure", "normal", "pure"]);
        var includeIngots = new OptionAction(OptionKind.IncludeIngots, "--include-ingots", ArgumentCount: 0, Choices: null);

        // argparse registration order: the automatic -h/--help first, then the add_argument calls.
        _optionStrings =
        [
            new("-h", help),
            new("--help", help),
            new("--data", data),
            new("--out", output),
            new("--miner", miner),
            new("--purity", purity),
            new("--include-ingots", includeIngots),
        ];
        _optionStringActions = new Dictionary<string, OptionAction>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, OptionAction> pair in _optionStrings)
        {
            _optionStringActions.Add(pair.Key, pair.Value);
        }
    }

    private enum OptionKind
    {
        Help,
        Data,
        Output,
        Miner,
        Purity,
        IncludeIngots,
    }

    /// <summary>Parses the command-line arguments.</summary>
    /// <param name="args">The arguments, without the program name.</param>
    /// <returns>
    /// The options (defaults: <see cref="PlannerConstants.DefaultDataPath"/>, <see cref="PlannerConstants.DefaultOutputFolder"/>,
    /// <see cref="MinerTier.Mk1"/>, <see cref="NodePurity.Normal"/>, no ingots), a help request, or an argparse error message.
    /// </returns>
    public CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        for (int i = 0; i < args.Count; i++)
        {
            if (args[i] is null)
            {
                throw new ArgumentException("Command-line arguments must not be null.", nameof(args));
            }
        }

        // Classify every argument: 'O' option, 'A' argument, '-' for the first "--" (everything after it is 'A').
        var pattern = new char[args.Count];
        var optionIndices = new Dictionary<int, IReadOnlyList<OptionMatch>>();
        int maxOptionIndex = -1;
        for (int i = 0; i < args.Count; i++)
        {
            if (args[i] == "--")
            {
                pattern[i] = '-';
                for (int j = i + 1; j < args.Count; j++)
                {
                    pattern[j] = 'A';
                }

                break;
            }

            IReadOnlyList<OptionMatch>? matches = ParseOptional(args[i]);
            if (matches is null)
            {
                pattern[i] = 'A';
            }
            else
            {
                pattern[i] = 'O';
                optionIndices[i] = matches;
                maxOptionIndex = i;
            }
        }

        var state = new ParseState();
        int start = 0;
        while (start <= maxOptionIndex)
        {
            int next = start;
            while (next <= maxOptionIndex && !optionIndices.ContainsKey(next))
            {
                next++;
            }

            // There are no positionals, so everything before the next option is an extra argument.
            if (!optionIndices.ContainsKey(start))
            {
                for (int i = start; i < next; i++)
                {
                    state.Extras.Add(args[i]);
                }

                start = next;
            }

            if (!ConsumeOptional(args, pattern, optionIndices[start], start, state, out start, out string? error))
            {
                return new CommandLineParseResult(null, false, error);
            }

            if (state.HelpRequested)
            {
                return new CommandLineParseResult(null, true, null);
            }
        }

        for (int i = start; i < args.Count; i++)
        {
            state.Extras.Add(args[i]);
        }

        if (state.Extras.Count > 0)
        {
            return new CommandLineParseResult(null, false, "unrecognized arguments: " + string.Join(' ', state.Extras));
        }

        var options = new PlannerOptions(
            state.DataPath,
            state.OutputFolder,
            ToMinerTier(state.Miner),
            ToNodePurity(state.Purity),
            state.IncludeIngots);
        return new CommandLineParseResult(options, false, null);
    }

    private static MinerTier ToMinerTier(string value) => value switch
    {
        "mk1" => MinerTier.Mk1,
        "mk2" => MinerTier.Mk2,
        "mk3" => MinerTier.Mk3,
        _ => throw new InvalidOperationException("Unvalidated miner choice: " + value),
    };

    private static NodePurity ToNodePurity(string value) => value switch
    {
        "impure" => NodePurity.Impure,
        "normal" => NodePurity.Normal,
        "pure" => NodePurity.Pure,
        _ => throw new InvalidOperationException("Unvalidated purity choice: " + value),
    };

    // argparse ArgumentError(action, message): "argument <option strings joined by '/'>: <message>".
    private static string ArgumentError(OptionAction action, string message) => $"argument {action.Name}: {message}";

    private static bool NegativeNumberLike(string arg)
    {
        // argparse _negative_number_matcher: re.match(r'-\.?\d', arg).
        int index = 1;
        if (index < arg.Length && arg[index] == '.')
        {
            index++;
        }

        return index < arg.Length && char.IsDigit(arg[index]);
    }

    // Python repr() of a str: single quotes unless the text contains ' and no ", backslash escapes for the quote,
    // backslash, \t \n \r, and \xhh / \uhhhh / \Uhhhhhhhh for non-printable characters.
    private static string PythonRepr(string text)
    {
        char quote = text.Contains('\'', StringComparison.Ordinal) && !text.Contains('"', StringComparison.Ordinal)
            ? '"'
            : '\'';
        var builder = new StringBuilder(text.Length + 2);
        builder.Append(quote);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int codePoint = c;
            int width = 1;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[i + 1]);
                width = 2;
            }

            if (c == quote || c == '\\')
            {
                builder.Append('\\').Append(c);
            }
            else if (c == '\t')
            {
                builder.Append("\\t");
            }
            else if (c == '\n')
            {
                builder.Append("\\n");
            }
            else if (c == '\r')
            {
                builder.Append("\\r");
            }
            else if (!IsPrintable(codePoint))
            {
                string escape = codePoint switch
                {
                    < 0x100 => "\\x" + codePoint.ToString("x2", CultureInfo.InvariantCulture),
                    < 0x10000 => "\\u" + codePoint.ToString("x4", CultureInfo.InvariantCulture),
                    _ => "\\U" + codePoint.ToString("x8", CultureInfo.InvariantCulture),
                };
                builder.Append(escape);
            }
            else
            {
                builder.Append(text, i, width);
            }

            i += width - 1;
        }

        builder.Append(quote);
        return builder.ToString();
    }

    // Python str.isprintable() for one code point: everything except the "Other" and "Separator" categories, but the
    // ASCII space is printable.
    private static bool IsPrintable(int codePoint)
    {
        if (codePoint == ' ')
        {
            return true;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(codePoint);
        return category is not (UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator or UnicodeCategory.SpaceSeparator);
    }

    private static bool TakeAction(OptionAction action, string? value, ParseState state, out string? error)
    {
        error = null;
        if (action.Choices is not null && !action.Choices.Contains(value!, StringComparer.Ordinal))
        {
            string choices = string.Join(", ", action.Choices.Select(PythonRepr));
            error = ArgumentError(action, $"invalid choice: {PythonRepr(value!)} (choose from {choices})");
            return false;
        }

        switch (action.Kind)
        {
            case OptionKind.Help:
                state.HelpRequested = true;
                break;
            case OptionKind.Data:
                state.DataPath = value!;
                break;
            case OptionKind.Output:
                state.OutputFolder = value!;
                break;
            case OptionKind.Miner:
                state.Miner = value!;
                break;
            case OptionKind.Purity:
                state.Purity = value!;
                break;
            case OptionKind.IncludeIngots:
                state.IncludeIngots = true;
                break;
            default:
                throw new InvalidOperationException("Unknown option kind: " + action.Kind);
        }

        return true;
    }

    // argparse consume_optional: returns false with an error, otherwise sets stop to the index after the option and
    // its value. Actions are taken in order; parsing stops at -h (state.HelpRequested).
    private bool ConsumeOptional(
        IReadOnlyList<string> args,
        char[] pattern,
        IReadOnlyList<OptionMatch> matches,
        int start,
        ParseState state,
        out int stop,
        out string? error)
    {
        stop = start + 1;
        if (matches.Count > 1)
        {
            string options = string.Join(", ", matches.Select(m => m.OptionString));
            error = $"ambiguous option: {args[start]} could match {options}";
            return false;
        }

        (OptionAction? action, string optionString, string? separator, string? explicitArgument) = matches[0];
        var actions = new List<(OptionAction Action, string? Value)>();
        while (true)
        {
            if (action is null)
            {
                state.Extras.Add(args[start]);
                stop = start + 1;
                error = null;
                return true;
            }

            if (explicitArgument is not null)
            {
                if (action.ArgumentCount == 0 && optionString[1] != PrefixChar && explicitArgument.Length > 0)
                {
                    // A single-dash flag followed by more characters: "-hx" is "-h -x".
                    if (!string.IsNullOrEmpty(separator) || explicitArgument[0] == PrefixChar)
                    {
                        error = ArgumentError(action, "ignored explicit argument " + PythonRepr(explicitArgument));
                        return false;
                    }

                    actions.Add((action, null));
                    optionString = PrefixChar + explicitArgument[..1];
                    if (_optionStringActions.TryGetValue(optionString, out OptionAction? chained))
                    {
                        action = chained;
                        explicitArgument = explicitArgument[1..];
                        if (explicitArgument.Length == 0)
                        {
                            separator = null;
                            explicitArgument = null;
                        }
                        else if (explicitArgument[0] == '=')
                        {
                            separator = "=";
                            explicitArgument = explicitArgument[1..];
                        }
                        else
                        {
                            separator = string.Empty;
                        }
                    }
                    else
                    {
                        state.Extras.Add(PrefixChar + explicitArgument);
                        stop = start + 1;
                        break;
                    }
                }
                else if (action.ArgumentCount == 1)
                {
                    stop = start + 1;
                    actions.Add((action, explicitArgument));
                    break;
                }
                else
                {
                    error = ArgumentError(action, "ignored explicit argument " + PythonRepr(explicitArgument));
                    return false;
                }
            }
            else
            {
                int valueIndex = start + 1;
                if (action.ArgumentCount == 0)
                {
                    stop = valueIndex;
                    actions.Add((action, null));
                    break;
                }

                if (valueIndex >= args.Count || pattern[valueIndex] != 'A')
                {
                    error = ArgumentError(action, "expected one argument");
                    return false;
                }

                stop = valueIndex + 1;
                actions.Add((action, args[valueIndex]));
                break;
            }
        }

        foreach ((OptionAction Action, string? Value) taken in actions)
        {
            if (!TakeAction(taken.Action, taken.Value, state, out error))
            {
                return false;
            }

            if (state.HelpRequested)
            {
                return true;
            }
        }

        error = null;
        return true;
    }

    // argparse _parse_optional: null when the argument is a positional ('A'); otherwise the matching options
    // (an entry with a null action is an unknown option; more than one entry is an ambiguous abbreviation).
    private List<OptionMatch>? ParseOptional(string arg)
    {
        if (arg.Length == 0 || arg[0] != PrefixChar)
        {
            return null;
        }

        if (_optionStringActions.TryGetValue(arg, out OptionAction? exact))
        {
            return [new OptionMatch(exact, arg, null, null)];
        }

        if (arg.Length == 1)
        {
            return null;
        }

        int equals = arg.IndexOf('=', StringComparison.Ordinal);
        if (equals >= 0 && _optionStringActions.TryGetValue(arg[..equals], out OptionAction? withValue))
        {
            return [new OptionMatch(withValue, arg[..equals], "=", arg[(equals + 1)..])];
        }

        List<OptionMatch> matches = GetOptionTuples(arg);
        if (matches.Count > 0)
        {
            return matches;
        }

        if (NegativeNumberLike(arg) || arg.Contains(' ', StringComparison.Ordinal))
        {
            return null;
        }

        return [new OptionMatch(null, arg, null, null)];
    }

    // argparse _get_option_tuples with allow_abbrev=True.
    private List<OptionMatch> GetOptionTuples(string arg)
    {
        var result = new List<OptionMatch>();
        int equals = arg.IndexOf('=', StringComparison.Ordinal);
        string prefix = equals >= 0 ? arg[..equals] : arg;
        string? separator = equals >= 0 ? "=" : null;
        string? explicitArgument = equals >= 0 ? arg[(equals + 1)..] : null;

        if (arg[1] == PrefixChar)
        {
            // Long options are only split at the '='; any unique prefix is accepted.
            foreach (KeyValuePair<string, OptionAction> pair in _optionStrings)
            {
                if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(new OptionMatch(pair.Value, pair.Key, separator, explicitArgument));
                }
            }
        }
        else
        {
            // A single-character option may be followed directly by more characters ("-hx").
            string shortPrefix = arg[..2];
            string shortExplicitArgument = arg[2..];
            foreach (KeyValuePair<string, OptionAction> pair in _optionStrings)
            {
                if (pair.Key == shortPrefix)
                {
                    result.Add(new OptionMatch(pair.Value, pair.Key, string.Empty, shortExplicitArgument));
                }
                else if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(new OptionMatch(pair.Value, pair.Key, separator, explicitArgument));
                }
            }
        }

        return result;
    }

    private sealed record OptionAction(OptionKind Kind, string Name, int ArgumentCount, IReadOnlyList<string>? Choices);

    private readonly record struct OptionMatch(
        OptionAction? Action,
        string OptionString,
        string? Separator,
        string? ExplicitArgument);

    private sealed class ParseState
    {
        public string DataPath { get; set; } = PlannerConstants.DefaultDataPath;

        public string OutputFolder { get; set; } = PlannerConstants.DefaultOutputFolder;

        public string Miner { get; set; } = "mk1";

        public string Purity { get; set; } = "normal";

        public bool IncludeIngots { get; set; }

        public bool HelpRequested { get; set; }

        public List<string> Extras { get; } = [];
    }
}
