using Satisfactory.Planner.Cli.CommandLine;
using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Tests.Cli.CommandLine;

/// <summary>
/// Expected results were captured from Python 3.14 argparse configured like the script's <c>main()</c>
/// (with <c>prog="satisfactory-planner"</c>).
/// </summary>
[TestFixture]
public sealed class CommandLineOptionsParserTests
{
    private static readonly PlannerOptions Defaults = new(
        PlannerConstants.DefaultDataPath,
        PlannerConstants.DefaultOutputFolder,
        MinerTier.Mk1,
        NodePurity.Normal,
        IncludeIngots: false);

    private static IEnumerable<TestCaseData> SuccessCases()
    {
        yield return Case(Defaults with { DataPath = "x.json" }, "--data", "x.json");
        yield return Case(Defaults with { DataPath = "x.json" }, "--data=x.json");
        yield return Case(Defaults with { OutputFolder = "out" }, "--out", "out");
        yield return Case(Defaults with { OutputFolder = "out" }, "--out=out");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk1 }, "--miner", "mk1");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk2 }, "--miner", "mk2");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk3 }, "--miner=mk3");
        yield return Case(Defaults with { NodePurity = NodePurity.Impure }, "--purity", "impure");
        yield return Case(Defaults with { NodePurity = NodePurity.Normal }, "--purity=normal");
        yield return Case(Defaults with { NodePurity = NodePurity.Pure }, "--purity", "pure");
        yield return Case(Defaults with { IncludeIngots = true }, "--include-ingots");
        yield return Case(Defaults with { IncludeIngots = true }, "--include-ingots", "--include-ingots");

        // Unique-prefix abbreviations of long options.
        yield return Case(Defaults with { IncludeIngots = true }, "--inc");
        yield return Case(Defaults with { IncludeIngots = true }, "--i");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk2, IncludeIngots = true }, "--inc", "--miner", "mk2");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk2 }, "--m", "mk2");
        yield return Case(
            new PlannerOptions("x", "y", MinerTier.Mk1, NodePurity.Pure, true), "--d", "x", "--o", "y", "--p", "pure", "--i");
        yield return Case(Defaults with { DataPath = "q" }, "--dat=q");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk3, NodePurity = NodePurity.Impure }, "--pur", "impure", "--mi", "mk3");

        // A later repeated option overrides an earlier one.
        yield return Case(Defaults with { DataPath = "b" }, "--data", "a", "--data", "b");
        yield return Case(Defaults with { MinerTier = MinerTier.Mk3 }, "--miner", "mk2", "--miner", "mk3");

        // All options at once, in both forms.
        yield return Case(
            new PlannerOptions("x", "y", MinerTier.Mk3, NodePurity.Pure, false),
            "--data=x", "--out=y", "--miner=mk3", "--purity=pure");
        yield return Case(
            new PlannerOptions("d.json", "o", MinerTier.Mk2, NodePurity.Impure, true),
            "--include-ingots", "--purity", "impure", "--out", "o", "--miner", "mk2", "--data", "d.json");

        // Values that are taken verbatim.
        yield return Case(Defaults with { DataPath = string.Empty }, "--data=");
        yield return Case(Defaults with { DataPath = string.Empty }, "--data", string.Empty);
        yield return Case(Defaults with { DataPath = "-1" }, "--data", "-1");
        yield return Case(Defaults with { DataPath = "-1x" }, "--data", "-1x");
        yield return Case(Defaults with { DataPath = "-" }, "--data", "-");
        yield return Case(Defaults with { DataPath = "-x y" }, "--data", "-x y");
        yield return Case(Defaults with { DataPath = " x" }, "--data", " x");
        yield return Case(Defaults with { DataPath = "--out" }, "--data=--out");
        yield return Case(Defaults with { DataPath = "a=b" }, "--data=a=b");
        yield return Case(Defaults with { DataPath = "a b" }, "--dat=a b");
    }

    private static IEnumerable<TestCaseData> ErrorCases()
    {
        const string MinerChoices = "(choose from 'mk1', 'mk2', 'mk3')";
        const string PurityChoices = "(choose from 'impure', 'normal', 'pure')";
        const string AllLongOptions = "--help, --data, --out, --miner, --purity, --include-ingots";

        // Invalid choices.
        yield return Case($"argument --miner: invalid choice: 'mk4' {MinerChoices}", "--miner", "mk4");
        yield return Case($"argument --miner: invalid choice: 'MK2' {MinerChoices}", "--miner=MK2");
        yield return Case($"argument --miner: invalid choice: '' {MinerChoices}", "--miner", string.Empty);
        yield return Case($"argument --miner: invalid choice: ' mk1' {MinerChoices}", "--miner", " mk1");
        yield return Case($"argument --purity: invalid choice: 'x' {PurityChoices}", "--purity=x");
        yield return Case($"argument --miner: invalid choice: 'a\\\\b' {MinerChoices}", "--miner", "a\\b");
        yield return Case($"argument --miner: invalid choice: \"it's\" {MinerChoices}", "--miner", "it's");
        yield return Case($"argument --miner: invalid choice: '\"q\"' {MinerChoices}", "--miner", "\"q\"");
        yield return Case($"argument --miner: invalid choice: 'it\\'s\"q\"' {MinerChoices}", "--miner", "it's\"q\"");
        yield return Case($"argument --miner: invalid choice: 'a\\tb' {MinerChoices}", "--miner", "a\tb");
        yield return Case($"argument --miner: invalid choice: 'é' {MinerChoices}", "--miner", "é");
        yield return Case($"argument --miner: invalid choice: '☃' {MinerChoices}", "--miner", "☃");
        yield return Case($"argument --miner: invalid choice: 'a\\x00\\x7f\\u200b' {MinerChoices}", "--miner", "a\0\u007f​");
        yield return Case(
            $"argument --miner: invalid choice: '\U0001F600\\xa0\\ud800' {MinerChoices}", "--miner", "\U0001F600 \uD800");

        // Missing values: nothing follows, or the next argument looks like an option.
        yield return Case("argument --data: expected one argument", "--data");
        yield return Case("argument --out: expected one argument", "--out");
        yield return Case("argument --miner: expected one argument", "--miner");
        yield return Case("argument --data: expected one argument", "--data", "--out", "x");
        yield return Case("argument --out: expected one argument", "--out", "--data");
        yield return Case("argument --data: expected one argument", "--data", "-x");
        yield return Case("argument --data: expected one argument", "--data", "--", "x");
        yield return Case("argument --miner: expected one argument", "--miner", "--data", "x");
        yield return Case("argument --data: expected one argument", "--data=x", "--data");

        // Unrecognized arguments are collected and reported after everything else.
        yield return Case("unrecognized arguments: --bogus", "--bogus");
        yield return Case("unrecognized arguments: pos", "pos");
        yield return Case("unrecognized arguments: -x", "-x");
        yield return Case("unrecognized arguments: -d", "-d");
        yield return Case("unrecognized arguments: -", "-");
        yield return Case("unrecognized arguments: ---", "---");
        yield return Case("unrecognized arguments: -1", "-1");
        yield return Case("unrecognized arguments: -.5", "-.5");
        yield return Case("unrecognized arguments: -.x", "-.x");
        yield return Case("unrecognized arguments: -x y", "-x y");
        yield return Case("unrecognized arguments: --data x", "--data x");
        yield return Case("unrecognized arguments: -ih", "-ih");
        yield return Case("unrecognized arguments: --x=1", "--x=1");
        yield return Case("unrecognized arguments: --", "--");
        yield return Case("unrecognized arguments: -- a", "--", "a");
        yield return Case("unrecognized arguments: -- --data x", "--", "--data", "x");
        yield return Case("unrecognized arguments: -- y", "--data", "x", "--", "y");
        yield return Case("unrecognized arguments: b", "--data", "a", "b");
        yield return Case("unrecognized arguments: a b --q", "a", "b", "--q");
        yield return Case("unrecognized arguments: pos --bogus", "pos", "--bogus");
        yield return Case("unrecognized arguments: --bogus=3 zz", "--bogus=3", "zz");
        yield return Case("unrecognized arguments: --bogus --bogus2 pos", "--bogus", "--bogus2", "pos");
        yield return Case("unrecognized arguments: --bogus=b pos -- --x", "--data=a", "--bogus=b", "pos", "--", "--x");

        // Errors raised while parsing come before unrecognized arguments and before a later -h.
        yield return Case($"argument --miner: invalid choice: 'mk4' {MinerChoices}", "--bogus", "--miner", "mk4");
        yield return Case($"argument --miner: invalid choice: 'mk4' {MinerChoices}", "--miner", "mk4", "-h");
        yield return Case($"argument --miner: invalid choice: 'mk4' {MinerChoices}", "--miner", "mk4", "--data");
        yield return Case("argument --data: expected one argument", "--data", "--miner", "mk4");

        // Flags do not take values.
        yield return Case("argument --include-ingots: ignored explicit argument '1'", "--include-ingots=1");
        yield return Case("argument --include-ingots: ignored explicit argument ''", "--include-ingots=");
        yield return Case("argument --include-ingots: ignored explicit argument ''", "--i=");
        yield return Case("argument -h/--help: ignored explicit argument '3'", "--hel=3");
        yield return Case("argument -h/--help: ignored explicit argument 'x'", "--help=x");
        yield return Case("argument -h/--help: ignored explicit argument 'x'", "-h=x");
        yield return Case("argument -h/--help: ignored explicit argument ''", "-h=");
        yield return Case("argument -h/--help: ignored explicit argument '-'", "-h-");

        // Ambiguous abbreviations ("--" followed by '=' is a prefix of every long option).
        yield return Case($"ambiguous option: --=x could match {AllLongOptions}", "--=x");
        yield return Case($"ambiguous option: --= could match {AllLongOptions}", "--=");
    }

    private static IEnumerable<TestCaseData> HelpCases()
    {
        yield return Args("-h");
        yield return Args("--help");
        yield return Args("--he");
        yield return Args("--h");
        yield return Args("-hx");
        yield return Args("-hh");
        yield return Args("-hx=y");
        yield return Args("-h", "--bogus");
        yield return Args("--bogus", "-h");
        yield return Args("-h", "--miner", "mk4");
        yield return Args("--data", "x", "--help");
        yield return Args("pos", "-h", "--data");
    }

    [Test]
    public void Parse_NoArguments_ReturnsDefaults()
    {
        CommandLineParseResult result = new CommandLineOptionsParser().Parse([]);

        Assert.That(result, Is.EqualTo(new CommandLineParseResult(Defaults, false, null)));
        Assert.That(result.Options, Is.EqualTo(new PlannerOptions(
            "data/satisfactory_data.json", "tiers", MinerTier.Mk1, NodePurity.Normal, false)));
    }

    [TestCaseSource(nameof(SuccessCases))]
    public void Parse_ValidArguments_ReturnsOptions(string[] args, PlannerOptions expected)
    {
        CommandLineParseResult result = new CommandLineOptionsParser().Parse(args);

        Assert.That(result, Is.EqualTo(new CommandLineParseResult(expected, false, null)));
    }

    [TestCaseSource(nameof(ErrorCases))]
    public void Parse_InvalidArguments_ReturnsArgparseError(string[] args, string expectedError)
    {
        CommandLineParseResult result = new CommandLineOptionsParser().Parse(args);

        Assert.That(result, Is.EqualTo(new CommandLineParseResult(null, false, expectedError)));
    }

    [TestCaseSource(nameof(HelpCases))]
    public void Parse_Help_ReturnsHelpRequested(string[] args)
    {
        CommandLineParseResult result = new CommandLineOptionsParser().Parse(args);

        Assert.That(result, Is.EqualTo(new CommandLineParseResult(null, true, null)));
    }

    [Test]
    public void Parse_SameParserTwice_DoesNotCarryStateOver()
    {
        var parser = new CommandLineOptionsParser();

        _ = parser.Parse(["--miner", "mk3", "--include-ingots", "--bogus"]);
        CommandLineParseResult result = parser.Parse([]);

        Assert.That(result.Options, Is.EqualTo(Defaults));
    }

    [Test]
    public void Parse_NullArguments_Throws()
    {
        var parser = new CommandLineOptionsParser();

        Assert.That(() => parser.Parse(null!), Throws.ArgumentNullException);
        Assert.That(() => parser.Parse(["--data", null!]), Throws.ArgumentException);
    }

    private static TestCaseData Case(PlannerOptions expected, params string[] args) =>
        new TestCaseData(args, expected).SetArgDisplayNames(Display(args));

    private static TestCaseData Case(string expectedError, params string[] args) =>
        new TestCaseData(args, expectedError).SetArgDisplayNames(Display(args));

    private static TestCaseData Args(params string[] args) =>
        new TestCaseData(new object[] { args }).SetArgDisplayNames(Display(args));

    private static string Display(string[] args) =>
        "[" + string.Join(", ", args.Select(a => "\"" + a.Replace("\0", "\\0", StringComparison.Ordinal) + "\"")) + "]";
}
