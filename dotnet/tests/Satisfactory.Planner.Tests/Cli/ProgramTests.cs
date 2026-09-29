using Satisfactory.Planner.Cli;
using Satisfactory.Planner.Cli.CommandLine;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Cli;

[TestFixture]
public sealed class ProgramTests
{
    private static readonly string NewLine = Environment.NewLine;
    private static readonly string[] FirstTierFiles = ["tier_0.dot", "tier_0_order.csv", "tier_0_order.txt"];

    [TestCase("--help")]
    [TestCase("-h")]
    public async Task RunAsync_Help_PrintsHelpToStdoutAndExitsZero(string option)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync([option], stdout, stderr, new NullDotRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout.ToString(), Is.EqualTo(UsageText.FormatHelp(NewLine)));
            Assert.That(stderr.ToString(), Is.Empty);
        });
    }

    [Test]
    public async Task RunAsync_UsageError_PrintsUsageAndErrorToStderrAndExitsTwo()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(["--miner", "mk9"], stdout, stderr, new NullDotRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(2));
            Assert.That(stdout.ToString(), Is.Empty);
            Assert.That(
                stderr.ToString(),
                Is.EqualTo(UsageText.FormatError("argument --miner: invalid choice: 'mk9' (choose from 'mk1', 'mk2', 'mk3')", NewLine)));
        });
    }

    [Test]
    public async Task RunAsync_BadDataFile_CreatesTheOutputFolderFirstThenFailsWithExitCodeOne()
    {
        using var scratch = new CoreScratchDirectory();
        string data = scratch.WriteFile("data.json", "{ not json");
        string output = scratch.Combine("out", "nested");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(["--data", data, "--out", output], stdout, stderr, new NullDotRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(Directory.Exists(output), Is.True, "os.makedirs(out) runs before the data is loaded");
            Assert.That(Directory.GetFiles(output), Is.Empty);
            Assert.That(stdout.ToString(), Is.Empty);
            Assert.That(stderr.ToString(), Does.StartWith(data + " is not valid JSON").And.EndWith(NewLine));
            Assert.That(stderr.ToString().TrimEnd().Split('\n'), Has.Length.EqualTo(1), "one line");
        });
    }

    [Test]
    public async Task RunAsync_MissingDataFile_DownloadsItThroughTheInjectedHandler()
    {
        using var scratch = new CoreScratchDirectory();
        string data = scratch.Combine("downloads", "data.json");
        using var handler = new FakeHttpMessageHandler("{ not json either");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--data", data, "--out", scratch.Combine("out")], stdout, stderr, new NullDotRenderer(), handler);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests, Is.EqualTo(new[] { new Uri(PlannerConstants.DataUrl) }));
            Assert.That(stdout.ToString(), Is.EqualTo($"{data} not found; downloading {PlannerConstants.DataUrl}{NewLine}"));
            Assert.That(File.ReadAllText(data), Is.EqualTo("{ not json either"));
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(stderr.ToString(), Does.StartWith(data + " is not valid JSON"));
        });
    }

    [Test]
    public async Task RunAsync_GraphvizFailure_StopsAfterTheFirstDotFileAndExitsOne()
    {
        using var scratch = new CoreScratchDirectory();
        string output = scratch.Combine("out");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--data", RepositoryPaths.DataFile, "--out", output], stdout, stderr, new FailingDotRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(stderr.ToString(), Is.EqualTo("dot failed: syntax error" + NewLine));
            Assert.That(stdout.ToString(), Is.Empty);
            Assert.That(
                Directory.GetFiles(output).Select(Path.GetFileName).Order(StringComparer.Ordinal),
                Is.EqualTo(FirstTierFiles));
        });
    }

    [Test]
    public async Task RunAsync_OutputFolderIsAnExistingFile_FailsWithOneLineAndExitCodeOne()
    {
        // Python: os.makedirs raises FileExistsError, a traceback and exit status 1.
        using var scratch = new CoreScratchDirectory();
        string output = scratch.WriteFile("afile", "x");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--data", RepositoryPaths.DataFile, "--out", output], stdout, stderr, new NullDotRenderer());

        AssertOneLineFailure(exitCode, stdout, stderr);
    }

    [TestCase("--out=")]
    [TestCase("--data=")]
    public async Task RunAsync_EmptyPath_FailsWithOneLineAndExitCodeOne(string option)
    {
        using var scratch = new CoreScratchDirectory();
        using var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("no request expected"));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--out", scratch.Combine("out"), "--data", scratch.Combine("data.json"), option],
            stdout,
            stderr,
            new NullDotRenderer(),
            handler);

        AssertOneLineFailure(exitCode, stdout, stderr);
        Assert.Multiple(() =>
        {
            Assert.That(stderr.ToString(), Is.EqualTo($"{option.TrimEnd('=')}: No such file or directory: ''{NewLine}"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public async Task RunAsync_DataPathIsADirectory_FailsWithoutDownloadingAndExitsOne()
    {
        using var scratch = new CoreScratchDirectory();
        string data = scratch.Combine("adir");
        Directory.CreateDirectory(data);
        using var handler = new FakeHttpMessageHandler(_ => throw new AssertionException("no request expected"));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--data", data, "--out", scratch.Combine("out")], stdout, stderr, new NullDotRenderer(), handler);

        AssertOneLineFailure(exitCode, stdout, stderr);
        Assert.That(handler.CallCount, Is.Zero);
    }

    [Test]
    public async Task RunAsync_DownloadTimesOut_FailsWithOneLineAndExitCodeOne()
    {
        using var scratch = new CoreScratchDirectory();
        string data = scratch.Combine("data.json");
        using var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("timed out"));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        int exitCode = await Program.RunAsync(
            ["--data", data, "--out", scratch.Combine("out")], stdout, stderr, new NullDotRenderer(), handler);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(stdout.ToString(), Is.EqualTo($"{data} not found; downloading {PlannerConstants.DataUrl}{NewLine}"));
            Assert.That(stderr.ToString(), Does.StartWith($"Could not download {PlannerConstants.DataUrl}: "));
            Assert.That(stderr.ToString().TrimEnd().Split('\n'), Has.Length.EqualTo(1), "one line");
            Assert.That(File.Exists(data), Is.False);
        });
    }

    [Test]
    public void RunAsync_RejectsNulls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Program.RunAsync(null!, TextWriter.Null, TextWriter.Null), Throws.ArgumentNullException);
            Assert.That(() => Program.RunAsync([], null!, TextWriter.Null), Throws.ArgumentNullException);
            Assert.That(() => Program.RunAsync([], TextWriter.Null, null!), Throws.ArgumentNullException);
        });
    }

    private static void AssertOneLineFailure(int exitCode, StringWriter stdout, StringWriter stderr)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(stdout.ToString(), Is.Empty);
            Assert.That(stderr.ToString(), Is.Not.EqualTo(NewLine).And.EndWith(NewLine));
            Assert.That(stderr.ToString().TrimEnd().Split('\n'), Has.Length.EqualTo(1), "one line");
        });
    }

    /// <summary>A Graphviz renderer that is available and always fails with a two-line message.</summary>
    private sealed class FailingDotRenderer : IDotRenderer
    {
        public bool IsAvailable => true;

        public void RenderSvg(string dotPath, string svgPath) =>
            throw new GraphvizRenderException("dot failed:\nsyntax error\n");
    }
}
