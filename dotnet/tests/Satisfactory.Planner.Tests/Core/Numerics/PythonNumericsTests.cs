using System.Globalization;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Tests.TestVectors;

namespace Satisfactory.Planner.Tests.Core.Numerics;

[TestFixture]
public sealed class PythonNumericsTests
{
    private static IEnumerable<TestCaseData> UnpaddedFormatSpecs() =>
        PythonNumberVectors.Instance.FormatSpecs
            .Select((spec, index) => (spec, index))
            .Where(s => PythonNumberVectors.ParseFloatSpec(s.spec).Width == 0)
            .Select(s => new TestCaseData(s.spec, s.index).SetArgDisplayNames(s.spec));

    private static IEnumerable<TestCaseData> RoundDigits() =>
        PythonNumberVectors.Instance.RoundDigits
            .Select((digits, index) => new TestCaseData(digits, index).SetArgDisplayNames($"ndigits={digits}"));

    [TestCaseSource(nameof(UnpaddedFormatSpecs))]
    public void FormatFixed_MatchesPythonVectors(string spec, int specIndex)
    {
        (_, bool thousands, int decimals) = PythonNumberVectors.ParseFloatSpec(spec);

        PythonNumberVectors.AssertAllMatch(
            $"f\"{{x:{spec}}}\"",
            PythonNumberVectors.Instance.Floats.Select(v =>
                (v.Repr, v.Formatted[specIndex], PythonNumerics.FormatFixed(v.Value, decimals, thousands))));
    }

    [TestCaseSource(nameof(RoundDigits))]
    public void Round_MatchesPythonVectorsBitForBit(int digits, int digitsIndex)
    {
        PythonNumberVectors.AssertAllMatch(
            $"round(x, {digits})",
            PythonNumberVectors.Instance.Floats.Select(v =>
                (v.Repr,
                 Bits(PythonNumberVectors.ParseRepr(v.RoundedRepr[digitsIndex])),
                 Bits(PythonNumerics.Round(v.Value, digits)))));
    }

    [Test]
    public void Repr_MatchesPythonVectors()
    {
        PythonNumberVectors.AssertAllMatch(
            "repr(x)",
            PythonNumberVectors.Instance.Floats.Select(v => (v.Repr, v.Repr, PythonNumerics.Repr(v.Value))));
    }

    [Test]
    public void RoundThenRepr_MatchesPythonVectors()
    {
        var vectors = PythonNumberVectors.Instance;
        PythonNumberVectors.AssertAllMatch(
            "repr(round(x, n))",
            vectors.RoundDigits.SelectMany((digits, index) => vectors.Floats.Select(v =>
                ($"{v.Repr} n={digits}", v.RoundedRepr[index], PythonNumerics.Repr(PythonNumerics.Round(v.Value, digits))))));
    }

    [TestCase(0.5, 0, 0.0)]
    [TestCase(1.5, 0, 2.0)]
    [TestCase(2.5, 0, 2.0)]
    [TestCase(-2.5, 0, -2.0)]
    [TestCase(0.125, 2, 0.12)]
    [TestCase(0.375, 2, 0.38)]
    [TestCase(2.675, 2, 2.67)]
    [TestCase(1.005, 2, 1.0)]
    [TestCase(1234.5, 0, 1234.0)]
    [TestCase(12345.25, 1, 12345.2)]
    [TestCase(0.1 + 0.2, 1, 0.3)]
    [TestCase(16.5, 1, 16.5)]
    [TestCase(1e22, 6, 1e22)]
    [TestCase(123.4567894, 6, 123.456789)]
    public void Round_RoundsHalfToEvenOnTheExactValue(double value, int digits, double expected)
    {
        Assert.That(PythonNumerics.Round(value, digits), Is.EqualTo(expected));
    }

    [Test]
    public void Round_KeepsTheSignOfZero()
    {
        Assert.Multiple(() =>
        {
            Assert.That(double.IsNegative(PythonNumerics.Round(-0.04, 1)), Is.True, "round(-0.04, 1) is -0.0");
            Assert.That(double.IsNegative(PythonNumerics.Round(-0.0, 2)), Is.True, "round(-0.0, 2) is -0.0");
            Assert.That(double.IsNegative(PythonNumerics.Round(0.04, 1)), Is.False, "round(0.04, 1) is 0.0");
            Assert.That(double.IsNegative(PythonNumerics.Round(-0.5, 0)), Is.True, "round(-0.5, 0) is -0.0");
        });
    }

    [Test]
    public void Round_PassesNonFiniteValuesThrough()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PythonNumerics.Round(double.PositiveInfinity, 1), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PythonNumerics.Round(double.NegativeInfinity, 6), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PythonNumerics.Round(double.NaN, 2), Is.NaN);
        });
    }

    [Test]
    public void Round_BeyondPythonsDigitLimit_ReturnsTheValue()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PythonNumerics.Round(5e-324, 323), Is.EqualTo(0.0), "round(5e-324, 323) == 0.0");
            Assert.That(PythonNumerics.Round(5e-324, 324), Is.EqualTo(5e-324), "round(5e-324, 324) == 5e-324");
            Assert.That(PythonNumerics.Round(double.MaxValue, 0), Is.EqualTo(double.MaxValue));
        });
    }

    [Test]
    public void Round_NegativeDigits_Throws()
    {
        Assert.That(() => PythonNumerics.Round(1.0, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(0.0, 2, false, "0.00")]
    [TestCase(-0.0, 2, false, "-0.00")]
    [TestCase(-0.001, 2, false, "-0.00")]
    [TestCase(-1e-12, 0, false, "-0")]
    [TestCase(-0.4, 0, true, "-0")]
    [TestCase(2.5, 0, false, "2")]
    [TestCase(3.5, 0, false, "4")]
    [TestCase(0.125, 2, false, "0.12")]
    [TestCase(2.675, 2, false, "2.67")]
    [TestCase(999.5, 0, true, "1,000")]
    [TestCase(999999.5, 0, true, "1,000,000")]
    [TestCase(-1234567.891, 2, true, "-1,234,567.89")]
    [TestCase(123.456, 1, true, "123.5")]
    [TestCase(1e22, 0, true, "10,000,000,000,000,000,000,000")]
    [TestCase(1e-7, 2, false, "0.00")]
    [TestCase(5e-324, 3, false, "0.000")]
    [TestCase(16.0, 0, false, "16")]
    public void FormatFixed_FormatsLikePython(double value, int decimals, bool thousands, string expected)
    {
        Assert.That(PythonNumerics.FormatFixed(value, decimals, thousands), Is.EqualTo(expected));
    }

    [TestCase(double.PositiveInfinity, "inf")]
    [TestCase(double.NegativeInfinity, "-inf")]
    [TestCase(double.NaN, "nan")]
    public void FormatFixed_NonFinite_PrintsPythonNames(double value, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(PythonNumerics.FormatFixed(value, 2, thousands: false), Is.EqualTo(expected));
            Assert.That(PythonNumerics.FormatFixed(value, 0, thousands: true), Is.EqualTo(expected));
        });
    }

    [Test]
    public void FormatFixed_NegativeNaN_PrintsNan()
    {
        Assert.That(PythonNumerics.FormatFixed(-double.NaN, 2, thousands: false), Is.EqualTo("nan"));
    }

    [Test]
    public void FormatFixed_NegativeDecimals_Throws()
    {
        Assert.That(() => PythonNumerics.FormatFixed(1.0, -1, thousands: false), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(16.0, "16.0")]
    [TestCase(-30000.0, "-30000.0")]
    [TestCase(0.0, "0.0")]
    [TestCase(-0.0, "-0.0")]
    [TestCase(0.1, "0.1")]
    [TestCase(0.30000000000000004, "0.30000000000000004")]
    [TestCase(0.0001, "0.0001")]
    [TestCase(0.00001, "1e-05")]
    [TestCase(0.000015, "1.5e-05")]
    [TestCase(1e15, "1000000000000000.0")]
    [TestCase(9999999999999998.0, "9999999999999998.0")]
    [TestCase(1e16, "1e+16")]
    [TestCase(1.5e16, "1.5e+16")]
    [TestCase(-1e22, "-1e+22")]
    [TestCase(1e100, "1e+100")]
    [TestCase(5e-324, "5e-324")]
    [TestCase(double.MaxValue, "1.7976931348623157e+308")]
    [TestCase(123456789.987, "123456789.987")]
    [TestCase(double.PositiveInfinity, "inf")]
    [TestCase(double.NegativeInfinity, "-inf")]
    [TestCase(double.NaN, "nan")]
    public void Repr_FormatsLikePython(double value, string expected)
    {
        Assert.That(PythonNumerics.Repr(value), Is.EqualTo(expected));
    }

    [TestCase(1.0, 2.0, 2.0)]
    [TestCase(2.0, 1.0, 2.0)]
    [TestCase(double.NaN, 1.0, double.NaN)]
    [TestCase(1.0, double.NaN, 1.0)]
    public void Max_UsesPythonSemantics(double a, double b, double expected)
    {
        Assert.That(PythonNumerics.Max(a, b), Is.EqualTo(expected));
    }

    [TestCase(1.0, 2.0, 1.0)]
    [TestCase(2.0, 1.0, 1.0)]
    [TestCase(double.NaN, 1.0, double.NaN)]
    [TestCase(1.0, double.NaN, 1.0)]
    public void Min_UsesPythonSemantics(double a, double b, double expected)
    {
        Assert.That(PythonNumerics.Min(a, b), Is.EqualTo(expected));
    }

    [Test]
    public void MaxAndMin_OnEqualValues_ReturnTheFirstArgument()
    {
        Assert.Multiple(() =>
        {
            Assert.That(double.IsNegative(PythonNumerics.Max(-0.0, 0.0)), Is.True, "max(-0.0, 0.0) is -0.0");
            Assert.That(double.IsNegative(PythonNumerics.Max(0.0, -0.0)), Is.False, "max(0.0, -0.0) is 0.0");
            Assert.That(double.IsNegative(PythonNumerics.Min(-0.0, 0.0)), Is.True, "min(-0.0, 0.0) is -0.0");
            Assert.That(double.IsNegative(PythonNumerics.Min(0.0, -0.0)), Is.False, "min(0.0, -0.0) is 0.0");
        });
    }

    [Test]
    public void Sum_MatchesPythonVectorsBitForBit()
    {
        var vectors = PythonSumVectors.Instance.Sums;

        // Compared as bit patterns: a sum that Python returns as the int 0 (empty list, only int zeros) is 0.0 here.
        Assert.That(vectors, Has.Count.GreaterThan(1000));
        PythonNumberVectors.AssertAllMatch(
            "sum(values)",
            vectors.Select(v => (
                $"{v.Description} = {v.Sum}",
                Bits(PythonNumberVectors.ParseRepr(v.Sum)),
                Bits(PythonNumerics.Sum(v.Doubles)))));
    }

    [Test]
    public void Sum_IsCompensatedNotLeftToRight()
    {
        double[] tenths = [0.1, 0.2, 0.3];
        double plain = 0.0;
        foreach (double x in tenths)
        {
            plain += x;
        }

        Assert.Multiple(() =>
        {
            Assert.That(PythonNumerics.Sum(tenths), Is.EqualTo(0.6), "sum([0.1, 0.2, 0.3])");
            Assert.That(plain, Is.EqualTo(0.6000000000000001), "plain left-to-right addition differs");
            Assert.That(PythonNumerics.Sum([1.0, 1e16, -1e16]), Is.EqualTo(1.0), "sum([1.0, 1e16, -1e16])");
            Assert.That(PythonNumerics.Sum([1e16, 1.0, 1.0]), Is.EqualTo(1.0000000000000002e16), "sum([1e16, 1.0, 1.0])");
        });
    }

    [Test]
    public void Sum_OfNothing_IsZero()
    {
        Assert.That(Bits(PythonNumerics.Sum([])), Is.EqualTo(Bits(0.0)));
    }

    [Test]
    public void Sum_StartsFromTheIntZero_SoANegativeZeroBecomesPositive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Bits(PythonNumerics.Sum([-0.0])), Is.EqualTo(Bits(0.0)), "0 + -0.0 is 0.0");
            Assert.That(Bits(PythonNumerics.Sum([-1.0, 1.0, -0.0])), Is.EqualTo(Bits(0.0)));
        });
    }

    [Test]
    public void Sum_KeepsInfinitiesAndNaN()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PythonNumerics.Sum([double.PositiveInfinity, 1.0]), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PythonNumerics.Sum([1.0, double.NegativeInfinity]), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PythonNumerics.Sum([1e308, 1e308, -1e308]), Is.EqualTo(double.PositiveInfinity), "overflow stays inf");
            Assert.That(PythonNumerics.Sum([double.PositiveInfinity, double.NegativeInfinity]), Is.NaN);
            Assert.That(PythonNumerics.Sum([1.0, double.NaN]), Is.NaN);
        });
    }

    [Test]
    public void Sum_EnumeratesTheSequenceOnce()
    {
        int enumerations = 0;
        IEnumerable<double> Values()
        {
            enumerations++;
            yield return 0.5;
            yield return 0.25;
        }

        double sum = PythonNumerics.Sum(Values());

        Assert.Multiple(() =>
        {
            Assert.That(sum, Is.EqualTo(0.75));
            Assert.That(enumerations, Is.EqualTo(1));
        });
    }

    [Test]
    public void Sum_Null_Throws()
    {
        Assert.That(() => PythonNumerics.Sum(null!), Throws.ArgumentNullException);
    }

    private static string Bits(double value) =>
        double.IsNaN(value)
            ? "nan"
            : BitConverter.DoubleToUInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);
}
