using System.Text;
using Satisfactory.Planner.Reporting.Formatting;

namespace Satisfactory.Planner.Tests.Reporting.Formatting;

/// <summary>
/// Expected strings were produced by CPython 3.14 <c>csv.writer(io.StringIO()).writerow(fields)</c>.
/// </summary>
[TestFixture]
public sealed class CsvFormatterTests
{
    private readonly CsvFormatter _formatter = new();

    [Test]
    public void FormatRow_PlainFields_AreJoinedWithCommasAndTerminatedWithCrLf()
    {
        Assert.That(_formatter.FormatRow(["a", "b", "c"]), Is.EqualTo("a,b,c\r\n"));
    }

    [TestCase("a,b", "\"a,b\"")]
    [TestCase("q\"x", "\"q\"\"x\"")]
    [TestCase("\"", "\"\"\"\"")]
    [TestCase("cr\rx", "\"cr\rx\"")]
    [TestCase("lf\nx", "\"lf\nx\"")]
    [TestCase("crlf\r\nx", "\"crlf\r\nx\"")]
    [TestCase(" lead", " lead")]
    [TestCase("trail ", "trail ")]
    [TestCase(" ", " ")]
    [TestCase("tab\tx", "tab\tx")]
    [TestCase("semi; colon", "semi; colon")]
    [TestCase("#x", "#x")]
    [TestCase("'single'", "'single'")]
    [TestCase("é", "é")]
    public void FormatRow_QuotesMinimallyLikePython(string field, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(_formatter.FormatRow([field, "z"]), Is.EqualTo(expected + ",z\r\n"));
            Assert.That(_formatter.FormatRow([field]), Is.EqualTo(expected + "\r\n"));
        });
    }

    [Test]
    public void FormatRow_EmptyFieldsAmongOthers_AreWrittenAsNothing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_formatter.FormatRow(["", ""]), Is.EqualTo(",\r\n"));
            Assert.That(_formatter.FormatRow(["a", ""]), Is.EqualTo("a,\r\n"));
            Assert.That(_formatter.FormatRow(["", "a", "", ""]), Is.EqualTo(",a,,\r\n"));
        });
    }

    [Test]
    public void FormatRow_SingleEmptyField_IsQuoted()
    {
        Assert.That(_formatter.FormatRow([""]), Is.EqualTo("\"\"\r\n"));
    }

    [Test]
    public void FormatRow_NullField_IsWrittenLikePythonNone()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_formatter.FormatRow([null!]), Is.EqualTo("\"\"\r\n"));
            Assert.That(_formatter.FormatRow(["a", null!]), Is.EqualTo("a,\r\n"));
        });
    }

    [Test]
    public void FormatRow_NoFields_IsJustTheTerminator()
    {
        Assert.That(_formatter.FormatRow([]), Is.EqualTo("\r\n"));
    }

    [Test]
    public void Format_TerminatesEveryRowIncludingTheLast()
    {
        string[][] rows = [["step", "item"], ["1", "Iron Plate"], ["2", "Screw, small"]];

        Assert.That(_formatter.Format(rows), Is.EqualTo("step,item\r\n1,Iron Plate\r\n2,\"Screw, small\"\r\n"));
    }

    [Test]
    public void Format_NoRows_IsEmpty()
    {
        Assert.That(_formatter.Format([]), Is.Empty);
    }

    [Test]
    public void FormatAndFormatRow_RejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => _formatter.FormatRow(null!), Throws.ArgumentNullException);
            Assert.That(() => _formatter.Format(null!), Throws.ArgumentNullException);
            Assert.That(() => _formatter.Format([null!]), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void Format_ReproducesEveryGoldenCsvFile()
    {
        string golden = Path.Combine(FindRepositoryRoot(), "golden-output");
        string[] files = Directory.GetFiles(golden, "*.csv").Order(StringComparer.Ordinal).ToArray();
        Assert.That(files, Is.Not.Empty);

        Assert.Multiple(() =>
        {
            foreach (string file in files)
            {
                // The checkout's line endings depend on git settings; the program always writes "\r\n".
                string expected = File.ReadAllText(file, Encoding.UTF8).Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace("\n", "\r\n", StringComparison.Ordinal);
                string actual = _formatter.Format(ParseCsv(expected));
                Assert.That(actual, Is.EqualTo(expected), Path.GetFileName(file));
            }
        });
    }

    /// <summary>Minimal RFC 4180 reader for rows terminated by <c>"\r\n"</c> (quoted fields may span lines).</summary>
    private static List<string[]> ParseCsv(string text)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                rows.Add([.. row]);
                row.Clear();
                i++;
            }
            else
            {
                field.Append(c);
            }
        }

        return rows;
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? dir = new(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data")) && Directory.Exists(Path.Combine(dir.FullName, "golden-output")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("No folder containing both data/ and golden-output/ above the test directory.");
    }
}
