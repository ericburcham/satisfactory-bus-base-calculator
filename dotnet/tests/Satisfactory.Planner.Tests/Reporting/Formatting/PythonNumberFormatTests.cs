using Satisfactory.Planner.Reporting.Formatting;
using Satisfactory.Planner.Tests.TestVectors;

namespace Satisfactory.Planner.Tests.Reporting.Formatting;

[TestFixture]
public sealed class PythonNumberFormatTests
{
    private static IEnumerable<TestCaseData> FloatSpecs() =>
        PythonNumberVectors.Instance.FormatSpecs
            .Select((spec, index) => new TestCaseData(spec, index).SetArgDisplayNames(spec));

    private static IEnumerable<TestCaseData> IntSpecs() =>
        PythonNumberVectors.Instance.IntSpecs
            .Select((spec, index) => new TestCaseData(spec, index).SetArgDisplayNames(spec));

    private static IEnumerable<TestCaseData> RoundDigits() =>
        PythonNumberVectors.Instance.RoundDigits
            .Select((digits, index) => new TestCaseData(digits, index).SetArgDisplayNames($"ndigits={digits}"));

    [TestCaseSource(nameof(FloatSpecs))]
    public void FixedAndFixedThousands_MatchPythonVectors(string spec, int specIndex)
    {
        (int width, bool thousands, int decimals) = PythonNumberVectors.ParseFloatSpec(spec);

        PythonNumberVectors.AssertAllMatch(
            $"f\"{{x:{spec}}}\"",
            PythonNumberVectors.Instance.Floats.Select(v =>
                (v.Repr,
                 v.Formatted[specIndex],
                 thousands
                     ? PythonNumberFormat.FixedThousands(v.Value, decimals, width)
                     : PythonNumberFormat.Fixed(v.Value, decimals, width))));
    }

    [TestCaseSource(nameof(IntSpecs))]
    public void Integer_MatchesPythonVectors(string spec, int specIndex)
    {
        int width = PythonNumberVectors.ParseIntSpec(spec);

        PythonNumberVectors.AssertAllMatch(
            $"f\"{{n:{spec}}}\"",
            PythonNumberVectors.Instance.Ints.Select(v =>
                (v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                 v.Formatted[specIndex],
                 PythonNumberFormat.Integer(v.Value, width))));
    }

    [TestCaseSource(nameof(RoundDigits))]
    public void RoundRepr_MatchesPythonVectors(int digits, int digitsIndex)
    {
        PythonNumberVectors.AssertAllMatch(
            $"str(round(x, {digits}))",
            PythonNumberVectors.Instance.Floats.Select(v =>
                (v.Repr, v.RoundedRepr[digitsIndex], PythonNumberFormat.RoundRepr(v.Value, digits))));
    }

    [Test]
    public void Repr_MatchesPythonVectors()
    {
        PythonNumberVectors.AssertAllMatch(
            "repr(x)",
            PythonNumberVectors.Instance.Floats.Select(v => (v.Repr, v.Repr, PythonNumberFormat.Repr(v.Value))));
    }

    [TestCase(1.5, 2, 8, "    1.50")]
    [TestCase(-1.5, 2, 8, "   -1.50")]
    [TestCase(123456789.0, 2, 8, "123456789.00")]
    [TestCase(-0.001, 1, 7, "   -0.0")]
    [TestCase(1.5, 2, 0, "1.50")]
    public void Fixed_RightAlignsAndNeverTruncates(double value, int decimals, int width, string expected)
    {
        Assert.That(PythonNumberFormat.Fixed(value, decimals, width), Is.EqualTo(expected));
    }

    [TestCase(1234.5, 2, 10, "  1,234.50")]
    [TestCase(-1234567.891, 2, 10, "-1,234,567.89")]
    [TestCase(999999.5, 0, 13, "    1,000,000")]
    [TestCase(double.PositiveInfinity, 0, 10, "       inf")]
    public void FixedThousands_RightAlignsAndNeverTruncates(double value, int decimals, int width, string expected)
    {
        Assert.That(PythonNumberFormat.FixedThousands(value, decimals, width), Is.EqualTo(expected));
    }

    [TestCase(4L, 4, "   4")]
    [TestCase(-5L, 4, "  -5")]
    [TestCase(12345L, 3, "12345")]
    [TestCase(0L, 0, "0")]
    public void Integer_RightAlignsAndNeverTruncates(long value, int width, string expected)
    {
        Assert.That(PythonNumberFormat.Integer(value, width), Is.EqualTo(expected));
    }

    [TestCase(1000.0, 1, "1000.0")]
    [TestCase(16.5, 1, "16.5")]
    [TestCase(16.45, 1, "16.4")]
    [TestCase(-0.04, 1, "-0.0")]
    [TestCase(-30000.0, 1, "-30000.0")]
    public void RoundRepr_OfAFloat_PrintsAFloat(double value, int digits, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(PythonNumberFormat.RoundRepr(value, digits), Is.EqualTo(expected));
            Assert.That(PythonNumberFormat.RoundRepr(value, digits, isInteger: false), Is.EqualTo(expected));
        });
    }

    [TestCase(-30000.0, 1, "-30000")]
    [TestCase(4.0, 1, "4")]
    [TestCase(165.0, 1, "165")]
    [TestCase(0.0, 1, "0")]
    [TestCase(-0.0, 1, "0")]
    [TestCase(1e22, 1, "10000000000000000000000")]
    public void RoundRepr_OfAnInteger_PrintsTheIntegerUnchanged(double value, int digits, string expected)
    {
        Assert.That(PythonNumberFormat.RoundRepr(value, digits, isInteger: true), Is.EqualTo(expected));
    }

    [TestCase(1.5)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void RoundRepr_OfAnIntegerThatIsNotWhole_Throws(double value)
    {
        Assert.That(() => PythonNumberFormat.RoundRepr(value, 1, isInteger: true), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void RoundRepr_NegativeDigits_Throws()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => PythonNumberFormat.RoundRepr(4.0, -1, isInteger: true), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => PythonNumberFormat.RoundRepr(4.0, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        });
    }

    [TestCase(16.0, "16.0")]
    [TestCase(1e-05, "1e-05")]
    [TestCase(double.NegativeInfinity, "-inf")]
    public void Repr_DelegatesToPythonNumerics(double value, string expected)
    {
        Assert.That(PythonNumberFormat.Repr(value), Is.EqualTo(expected));
    }
}
