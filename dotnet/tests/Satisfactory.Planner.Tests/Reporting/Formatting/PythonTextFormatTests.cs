using Satisfactory.Planner.Reporting.Formatting;

namespace Satisfactory.Planner.Tests.Reporting.Formatting;

[TestFixture]
public sealed class PythonTextFormatTests
{
    [TestCase("Iron Plate", 12, "Iron Plate  ")]
    [TestCase("", 3, "   ")]
    [TestCase("Reinforced Iron Plate", 5, "Reinforced Iron Plate")]
    [TestCase("abc", 3, "abc")]
    [TestCase("abc", 0, "abc")]
    [TestCase("abc", -4, "abc")]
    public void PadRight_PadsOnTheRightAndNeverTruncates(string text, int width, string expected)
    {
        Assert.That(PythonTextFormat.PadRight(text, width), Is.EqualTo(expected));
    }

    [TestCase("new items", 9, "new items")]
    [TestCase("alts", 5, " alts")]
    [TestCase("", 2, "  ")]
    [TestCase("total machines", 4, "total machines")]
    [TestCase("x", 0, "x")]
    public void PadLeft_PadsOnTheLeftAndNeverTruncates(string text, int width, string expected)
    {
        Assert.That(PythonTextFormat.PadLeft(text, width), Is.EqualTo(expected));
    }

    [Test]
    public void Padding_CountsCodePointsLikePython()
    {
        const string text = "\U0001F3ED x";   // len() == 3 in Python: the surrogate pair is one code point

        Assert.Multiple(() =>
        {
            Assert.That(PythonTextFormat.PadRight(text, 5), Is.EqualTo(text + "  "));
            Assert.That(PythonTextFormat.PadLeft(text, 5), Is.EqualTo("  " + text));
            Assert.That(PythonTextFormat.PadRight("é", 3), Is.EqualTo("é  "));
        });
    }

    [Test]
    public void Padding_NullText_Throws()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => PythonTextFormat.PadRight(null!, 3), Throws.ArgumentNullException);
            Assert.That(() => PythonTextFormat.PadLeft(null!, 3), Throws.ArgumentNullException);
        });
    }
}
