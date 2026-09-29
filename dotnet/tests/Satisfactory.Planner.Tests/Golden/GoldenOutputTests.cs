using System.Text;
using Satisfactory.Planner.Cli;
using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Golden;

/// <summary>
/// End-to-end parity with the Python reference: runs the whole command line (<see cref="Program.RunAsync"/>) once with
/// the options that produced <c>golden-output/</c> (the defaults: <c>--miner mk1 --purity normal</c>, no ingots, data
/// <c>data/satisfactory_data.json</c>) into a fresh temporary folder, and compares every non-<c>.svg</c> file with the
/// golden one.
/// </summary>
/// <remarks>
/// <para>
/// Line endings are normalised (<c>\r\n</c> to <c>\n</c>) on both sides before comparing: the golden files were written
/// by Python on Windows (text files in text mode, so CRLF; CSV files by <c>csv.writer</c>, always CRLF), but the line
/// endings of the checkout depend on git's <c>core.autocrlf</c>, not on the program. The program's own line endings are
/// pinned separately (<see cref="ProducedFiles_UseCrLfForCsvAndThePlatformNewLineForText"/>).
/// </para>
/// <para>
/// <c>.dot</c> files are compared as multisets of lines: the Python writes the <c>in_*</c> input nodes by iterating a
/// <c>set</c>, so their line order is arbitrary. Every other file must be identical after the normalisation.
/// </para>
/// <para>
/// Graphviz is not needed: a <see cref="NullDotRenderer"/> is injected, so no <c>.svg</c> is produced (the golden
/// <c>.svg</c> files depend on the installed Graphviz version and are not compared).
/// </para>
/// </remarks>
[TestFixture]
[Category("Golden")]
public sealed class GoldenOutputTests
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private string _outputFolder = string.Empty;
    private int _exitCode;
    private string _stdout = string.Empty;
    private string _stderr = string.Empty;

    [OneTimeSetUp]
    public async Task RunTheCommandLineOnce()
    {
        _outputFolder = Path.Combine(Path.GetTempPath(), "satisfactory-planner-golden-" + Guid.NewGuid().ToString("N"));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        _exitCode = await Program.RunAsync(
            ["--data", RepositoryPaths.DataFile, "--out", _outputFolder], stdout, stderr, new NullDotRenderer());
        _stdout = stdout.ToString();
        _stderr = stderr.ToString();
    }

    [OneTimeTearDown]
    public void DeleteTheOutput()
    {
        try
        {
            Directory.Delete(_outputFolder, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary folder.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup, as above.
        }
    }

    /// <summary>Every non-<c>.svg</c> file of <c>golden-output/</c>, one test case each.</summary>
    public static IEnumerable<TestCaseData> GoldenFiles() =>
        NonSvgFileNames(RepositoryPaths.GoldenOutput).Select(name => new TestCaseData(name).SetArgDisplayNames(name));

    [Test]
    public void Run_SucceedsWithNothingOnStderr()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_exitCode, Is.Zero, () => "stderr: " + _stderr);
            Assert.That(_stderr, Is.Empty);
        });
    }

    [Test]
    public void Stdout_IsTheSummary()
    {
        string summary = File.ReadAllText(Path.Combine(_outputFolder, "summary.txt"), StrictUtf8);

        Assert.Multiple(() =>
        {
            Assert.That(_stdout, Is.EqualTo(summary), "stdout is exactly the written summary.txt");
            Assert.That(
                Normalise(_stdout),
                Is.EqualTo(Normalise(File.ReadAllText(Path.Combine(RepositoryPaths.GoldenOutput, "summary.txt"), StrictUtf8))),
                "stdout is the golden summary");
        });
    }

    [Test]
    public void ProducedFiles_AreExactlyTheGoldenNonSvgFiles()
    {
        var golden = NonSvgFileNames(RepositoryPaths.GoldenOutput);
        var produced = NonSvgFileNames(_outputFolder);

        Assert.Multiple(() =>
        {
            Assert.That(golden, Is.Not.Empty);
            Assert.That(produced.Except(golden, StringComparer.Ordinal), Is.Empty, "files not in golden-output");
            Assert.That(golden.Except(produced, StringComparer.Ordinal), Is.Empty, "golden files not produced");
            Assert.That(Directory.GetFiles(_outputFolder, "*.svg"), Is.Empty, "no .svg without Graphviz");
        });
    }

    [TestCaseSource(nameof(GoldenFiles))]
    public void ProducedFile_MatchesGolden(string fileName)
    {
        string producedPath = Path.Combine(_outputFolder, fileName);
        Assert.That(File.Exists(producedPath), Is.True, $"{fileName} was not produced");
        string expected = Normalise(File.ReadAllText(Path.Combine(RepositoryPaths.GoldenOutput, fileName), StrictUtf8));
        string actual = Normalise(File.ReadAllText(producedPath, StrictUtf8));

        string? difference = fileName.EndsWith(".dot", StringComparison.Ordinal)
            ? LineMultisetDifference(expected, actual)
            : FirstLineDifference(expected, actual);

        Assert.That(difference, Is.Null, () => $"{fileName}: {difference}");
    }

    [Test]
    public void ProducedFiles_UseCrLfForCsvAndThePlatformNewLineForText()
    {
        var files = NonSvgFileNames(_outputFolder);
        Assert.That(files, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (string name in files)
            {
                string content = File.ReadAllText(Path.Combine(_outputFolder, name), StrictUtf8);
                string newLine = name.EndsWith(".csv", StringComparison.Ordinal) ? "\r\n" : Environment.NewLine;
                string rest = content.Replace(newLine, string.Empty, StringComparison.Ordinal);
                Assert.That(content, Does.EndWith(newLine), $"{name} ends with its line terminator");
                Assert.That(rest.IndexOfAny(['\r', '\n']), Is.EqualTo(-1), $"{name} uses only {Escape(newLine)} line endings");
            }
        });
    }

    private static List<string> NonSvgFileNames(string folder) =>
        Directory.GetFiles(folder)
            .Select(path => Path.GetFileName(path))
            .Where(name => !name.EndsWith(".svg", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    private static string Normalise(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string[] Lines(string normalised) => normalised.Split('\n');

    /// <summary>The first differing line (1-based) with both versions, or <see langword="null"/> when equal.</summary>
    private static string? FirstLineDifference(string expected, string actual)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return null;
        }

        string[] expectedLines = Lines(expected);
        string[] actualLines = Lines(actual);
        for (int index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
        {
            string? e = index < expectedLines.Length ? expectedLines[index] : null;
            string? a = index < actualLines.Length ? actualLines[index] : null;
            if (!string.Equals(e, a, StringComparison.Ordinal))
            {
                return $"first difference at line {index + 1}:{Environment.NewLine}"
                    + $"  golden:   {Show(e)}{Environment.NewLine}"
                    + $"  produced: {Show(a)}";
            }
        }

        return "the files differ in a way the line comparison cannot show";
    }

    /// <summary>The lines missing from and extra in <paramref name="actual"/> (as multisets), or <see langword="null"/>.</summary>
    private static string? LineMultisetDifference(string expected, string actual)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string line in Lines(expected))
        {
            counts[line] = counts.GetValueOrDefault(line) + 1;
        }

        foreach (string line in Lines(actual))
        {
            counts[line] = counts.GetValueOrDefault(line) - 1;
        }

        var missing = counts.Where(c => c.Value > 0).SelectMany(c => Enumerable.Repeat(c.Key, c.Value)).ToList();
        var extra = counts.Where(c => c.Value < 0).SelectMany(c => Enumerable.Repeat(c.Key, -c.Value)).ToList();
        if (missing.Count == 0 && extra.Count == 0)
        {
            return null;
        }

        var text = new StringBuilder("the line multisets differ.");
        foreach (string line in missing)
        {
            text.Append(Environment.NewLine).Append("  missing: ").Append(Show(line));
        }

        foreach (string line in extra)
        {
            text.Append(Environment.NewLine).Append("  extra:   ").Append(Show(line));
        }

        return text.ToString();
    }

    private static string Show(string? line) => line is null ? "<end of file>" : "'" + line + "'";

    private static string Escape(string newLine) => newLine.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
}
