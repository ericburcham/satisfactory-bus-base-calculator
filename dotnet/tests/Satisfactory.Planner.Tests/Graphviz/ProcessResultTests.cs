using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class ProcessResultTests
{
    [Test]
    public void Record_ExposesValuesAndHasValueEquality()
    {
        var result = new ProcessResult(1, "out", "err");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.StdOut, Is.EqualTo("out"));
            Assert.That(result.StdErr, Is.EqualTo("err"));
            Assert.That(result, Is.EqualTo(new ProcessResult(1, "out", "err")));
        });
    }
}
