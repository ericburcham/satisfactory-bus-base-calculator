using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.IO;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class ReportWriterBaseTests
{
    [Test]
    public void Write_WritesRenderedContentToFileNameInsideTheOutputFolder()
    {
        var fileSystem = new InMemoryFileSystem();
        TierReportContext context = MinimalContext() with { OutputFolder = Path.Combine("reports", "run") };

        new EchoWriter(fileSystem).Write(Minimal(), context);

        Assert.That(
            fileSystem.ReadAllText(Path.Combine("reports", "run", "echo_0.txt")),
            Is.EqualTo("tier 0 with 1 machines\r\n"));
    }

    [Test]
    public void Write_ReplacesAnExistingFile()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.AddFile(Path.Combine(OutputFolder, "echo_0.txt"), "old");

        new EchoWriter(fileSystem).Write(Minimal(), MinimalContext());

        Assert.That(fileSystem.ReadAllText(Path.Combine(OutputFolder, "echo_0.txt")), Is.EqualTo("tier 0 with 1 machines\r\n"));
    }

    [Test]
    public void ConstructorAndWrite_RejectNulls()
    {
        var writer = new EchoWriter(new InMemoryFileSystem());

        Assert.Multiple(() =>
        {
            Assert.That(() => new EchoWriter(null!), Throws.ArgumentNullException);
            Assert.That(() => writer.Write(null!, MinimalContext()), Throws.ArgumentNullException);
            Assert.That(() => writer.Write(Minimal(), null!), Throws.ArgumentNullException);
        });
    }

    private sealed class EchoWriter(IFileSystem fileSystem) : ReportWriterBase(fileSystem)
    {
        protected override string FileName(TierResult result) => $"echo_{result.Tier}.txt";

        protected override string Render(TierResult result, TierReportContext context) =>
            $"tier {result.Tier} with {context.Totals.NewMachines} machines\r\n";
    }
}
