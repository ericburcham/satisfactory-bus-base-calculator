using Satisfactory.Planner.Cli.CommandLine;

namespace Satisfactory.Planner.Tests.Cli.CommandLine;

[TestFixture]
public sealed class UsageTextTests
{
    // `python python/satisfactory_plan_tiers.py --help` (Python 3.14, NO_COLOR=1, not a terminal: 80 columns).
    private const string PythonScriptHelp =
        "usage: satisfactory_plan_tiers.py [-h] [--data DATA] [--out OUT]\n" +
        "                                  [--miner {mk1,mk2,mk3}]\n" +
        "                                  [--purity {impure,normal,pure}]\n" +
        "                                  [--include-ingots]\n" +
        "\n" +
        "options:\n" +
        "  -h, --help            show this help message and exit\n" +
        "  --data DATA           path to the game data file (downloaded there if\n" +
        "                        missing)\n" +
        "  --out OUT             folder for the output files (created if missing)\n" +
        "  --miner {mk1,mk2,mk3}\n" +
        "  --purity {impure,normal,pure}\n" +
        "  --include-ingots\n";

    // The same parser built with ArgumentParser(prog="satisfactory-planner"): format_help(), which argparse
    // reflows for the shorter program name.
    private const string ArgparseHelpForNewProgramName =
        "usage: satisfactory-planner [-h] [--data DATA] [--out OUT]\n" +
        "                            [--miner {mk1,mk2,mk3}]\n" +
        "                            [--purity {impure,normal,pure}] [--include-ingots]\n" +
        "\n" +
        "options:\n" +
        "  -h, --help            show this help message and exit\n" +
        "  --data DATA           path to the game data file (downloaded there if\n" +
        "                        missing)\n" +
        "  --out OUT             folder for the output files (created if missing)\n" +
        "  --miner {mk1,mk2,mk3}\n" +
        "  --purity {impure,normal,pure}\n" +
        "  --include-ingots\n";

    [Test]
    public void ProgramName_IsSatisfactoryPlanner()
    {
        Assert.That(UsageText.ProgramName, Is.EqualTo("satisfactory-planner"));
    }

    [Test]
    public void Help_EqualsArgparseHelpForTheNewProgramName()
    {
        Assert.That(UsageText.Help, Is.EqualTo(ArgparseHelpForNewProgramName));
    }

    [Test]
    public void Help_OptionsSectionEqualsThePythonScriptHelp()
    {
        static string OptionsSection(string help) => help[help.IndexOf("\n\noptions:\n", StringComparison.Ordinal)..];

        Assert.That(OptionsSection(UsageText.Help), Is.EqualTo(OptionsSection(PythonScriptHelp)));
    }

    [Test]
    public void Usage_IsTheFirstPartOfHelp()
    {
        Assert.That(UsageText.Usage, Is.EqualTo(
            "usage: satisfactory-planner [-h] [--data DATA] [--out OUT]\n" +
            "                            [--miner {mk1,mk2,mk3}]\n" +
            "                            [--purity {impure,normal,pure}] [--include-ingots]\n"));
        Assert.That(UsageText.Help, Does.StartWith(UsageText.Usage + "\n"));
    }

    [Test]
    public void Help_NoLineIsWiderThanArgparseWidth()
    {
        string[] lines = UsageText.Help.TrimEnd('\n').Split('\n');

        Assert.That(lines.Max(line => line.Length), Is.LessThanOrEqualTo(78));
    }

    [Test]
    public void FormatError_IsUsageThenProgramError()
    {
        string text = UsageText.FormatError("argument --data: expected one argument");

        Assert.That(text, Is.EqualTo(
            "usage: satisfactory-planner [-h] [--data DATA] [--out OUT]\n" +
            "                            [--miner {mk1,mk2,mk3}]\n" +
            "                            [--purity {impure,normal,pure}] [--include-ingots]\n" +
            "satisfactory-planner: error: argument --data: expected one argument\n"));
    }

    [Test]
    public void FormatError_WithNewLine_ReplacesEveryLineFeed()
    {
        string text = UsageText.FormatError("unrecognized arguments: pos", "\r\n");

        Assert.That(text, Is.EqualTo(UsageText.FormatError("unrecognized arguments: pos").Replace("\n", "\r\n", StringComparison.Ordinal)));
        Assert.That(text, Does.EndWith("]\r\nsatisfactory-planner: error: unrecognized arguments: pos\r\n"));
        Assert.That(text.Replace("\r\n", string.Empty, StringComparison.Ordinal), Does.Not.Contain('\n'));
    }

    [Test]
    public void FormatHelp_WithNewLine_ReplacesEveryLineFeed()
    {
        Assert.That(UsageText.FormatHelp("\n"), Is.EqualTo(UsageText.Help));
        Assert.That(UsageText.FormatHelp("\r\n"), Is.EqualTo(UsageText.Help.Replace("\n", "\r\n", StringComparison.Ordinal)));
    }

    [Test]
    public void Format_NullArguments_Throw()
    {
        Assert.That(() => UsageText.FormatError(null!), Throws.ArgumentNullException);
        Assert.That(() => UsageText.FormatError("x", null!), Throws.ArgumentNullException);
        Assert.That(() => UsageText.FormatHelp(null!), Throws.ArgumentNullException);
    }
}
