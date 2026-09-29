using Satisfactory.Planner.Cli.CommandLine;
using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Tests.Cli.CommandLine;

[TestFixture]
public sealed class CommandLineParseResultTests
{
    [Test]
    public void Properties_ReturnConstructorValues()
    {
        var options = new PlannerOptions("d", "o", MinerTier.Mk2, NodePurity.Pure, true);

        var result = new CommandLineParseResult(options, false, null);

        Assert.That(result.Options, Is.SameAs(options));
        Assert.That(result.HelpRequested, Is.False);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void Equality_IsByValue()
    {
        var a = new CommandLineParseResult(new PlannerOptions("d", "o", MinerTier.Mk1, NodePurity.Normal, false), false, null);
        var b = new CommandLineParseResult(new PlannerOptions("d", "o", MinerTier.Mk1, NodePurity.Normal, false), false, null);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(new CommandLineParseResult(null, false, "x"), Is.Not.EqualTo(new CommandLineParseResult(null, false, "y")));
        Assert.That(new CommandLineParseResult(null, true, null), Is.Not.EqualTo(new CommandLineParseResult(null, false, null)));
    }
}
