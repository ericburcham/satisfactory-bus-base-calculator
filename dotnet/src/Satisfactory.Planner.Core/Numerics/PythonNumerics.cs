using System.Globalization;
using System.Numerics;
using System.Text;

namespace Satisfactory.Planner.Core.Numerics;

/// <summary>
/// Python float semantics that the planner's results and reports depend on: <c>round(x, n)</c>,
/// <c>f"{x:.{d}f}"</c> / <c>f"{x:,.{d}f}"</c>, <c>repr(x)</c> and the two-argument <c>max</c> / <c>min</c>.
/// </summary>
/// <remarks>
/// Rounding works on the exact binary value of the double with <see cref="BigInteger"/> arithmetic and rounds
/// half-to-even, exactly like CPython's correctly rounded <c>dtoa</c>. <see cref="Math.Round(double, int)"/> is not
/// equivalent.
/// </remarks>
public static class PythonNumerics
{
    /// <summary>
    /// CPython's <c>NDIGITS_MAX</c>: for more digits than this <c>round</c> returns the value unchanged.
    /// </summary>
    private const int MaxRoundDigits = 323;

    /// <summary>Python's decimal exponent limit above which <c>repr</c> switches to scientific notation.</summary>
    private const int ReprMaxPositionalExponent = 16;

    /// <summary>Python's decimal exponent limit below which <c>repr</c> switches to scientific notation.</summary>
    private const int ReprMinPositionalExponent = -4;

    /// <summary>
    /// Python <c>round(value, digits)</c> for <paramref name="digits"/> &gt;= 0: rounds the exact binary value
    /// half-to-even to <paramref name="digits"/> decimals and returns the double nearest to that decimal.
    /// </summary>
    /// <param name="value">The value to round. Infinities, NaN and zeros are returned unchanged.</param>
    /// <param name="digits">The number of decimals (0 or more).</param>
    /// <returns>The rounded value; the sign of a zero result follows the input, e.g. <c>round(-0.04, 1) == -0.0</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative.</exception>
    public static double Round(double value, int digits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(digits);
        if (digits > MaxRoundDigits || value == 0 || !double.IsFinite(value))
        {
            return value;
        }

        var exact = ExactDouble.Of(value);
        BigInteger scaled = exact.ScaledMagnitudeHalfEven(digits);
        double magnitude = scaled.IsZero
            ? 0.0
            : double.Parse(
                scaled.ToString(CultureInfo.InvariantCulture) + "E-" + digits.ToString(CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        return exact.IsNegative ? -magnitude : magnitude;
    }

    /// <summary>
    /// Python <c>f"{value:.{decimals}f}"</c>, or <c>f"{value:,.{decimals}f}"</c> when <paramref name="thousands"/>
    /// is set (no width).
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="decimals">The number of decimals (0 or more); 0 prints no decimal point.</param>
    /// <param name="thousands">Group the integer part in threes with <c>','</c>.</param>
    /// <returns>
    /// The formatted value. Negative values keep their <c>'-'</c> even when they round to zero (<c>-0.00</c>), and so
    /// does <c>-0.0</c>. Non-finite values print as <c>inf</c>, <c>-inf</c> and <c>nan</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="decimals"/> is negative.</exception>
    public static string FormatFixed(double value, int decimals, bool thousands)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        if (double.IsNaN(value))
        {
            return "nan";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "inf" : "-inf";
        }

        var exact = ExactDouble.Of(value);
        string digits = exact.ScaledMagnitudeHalfEven(decimals).ToString(CultureInfo.InvariantCulture);
        if (digits.Length <= decimals)
        {
            digits = new string('0', decimals + 1 - digits.Length) + digits;
        }

        string integerPart = digits[..^decimals];
        var text = new StringBuilder(digits.Length + (digits.Length / 3) + 2);
        if (exact.IsNegative)
        {
            text.Append('-');
        }

        if (thousands)
        {
            AppendGrouped(text, integerPart);
        }
        else
        {
            text.Append(integerPart);
        }

        if (decimals > 0)
        {
            text.Append('.').Append(digits, digits.Length - decimals, decimals);
        }

        return text.ToString();
    }

    /// <summary>
    /// Python <c>repr(value)</c> (= <c>str(value)</c>) of a float: the shortest digits that round-trip, positional
    /// with at least one digit after the point (<c>16.0</c>, <c>0.0001</c>, <c>-0.0</c>) when the decimal exponent is
    /// in [-4, 16), otherwise scientific (<c>1e-05</c>, <c>1.5e+16</c>); <c>inf</c>, <c>-inf</c>, <c>nan</c>.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The Python representation.</returns>
    public static string Repr(double value)
    {
        if (double.IsNaN(value))
        {
            return "nan";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "inf" : "-inf";
        }

        string sign = double.IsNegative(value) ? "-" : string.Empty;
        if (value == 0)
        {
            return sign + "0.0";
        }

        (string digits, int pointPosition) = ShortestDigits(Math.Abs(value));
        int exponent = pointPosition - 1;
        if (exponent < ReprMinPositionalExponent || exponent >= ReprMaxPositionalExponent)
        {
            string mantissa = digits.Length == 1 ? digits : digits[..1] + "." + digits[1..];
            string exponentText = Math.Abs(exponent).ToString("00", CultureInfo.InvariantCulture);
            return sign + mantissa + (exponent < 0 ? "e-" : "e+") + exponentText;
        }

        if (pointPosition <= 0)
        {
            return sign + "0." + new string('0', -pointPosition) + digits;
        }

        if (pointPosition >= digits.Length)
        {
            return sign + digits + new string('0', pointPosition - digits.Length) + ".0";
        }

        return sign + digits[..pointPosition] + "." + digits[pointPosition..];
    }

    /// <summary>Python <c>max(a, b)</c> on two floats: <c>b if b &gt; a else a</c> (NaN behaviour included).</summary>
    /// <param name="a">The first argument.</param>
    /// <param name="b">The second argument.</param>
    /// <returns><paramref name="b"/> when it is greater than <paramref name="a"/>; otherwise <paramref name="a"/>.</returns>
    public static double Max(double a, double b) => b > a ? b : a;

    /// <summary>Python <c>min(a, b)</c> on two floats: <c>b if b &lt; a else a</c> (NaN behaviour included).</summary>
    /// <param name="a">The first argument.</param>
    /// <param name="b">The second argument.</param>
    /// <returns><paramref name="b"/> when it is less than <paramref name="a"/>; otherwise <paramref name="a"/>.</returns>
    public static double Min(double a, double b) => b < a ? b : a;

    /// <summary>
    /// The shortest round-trip significant digits of a positive finite double (no leading or trailing zeros) and
    /// the position of the decimal point relative to them: value = 0.<c>digits</c> * 10^<c>pointPosition</c>.
    /// </summary>
    private static (string Digits, int PointPosition) ShortestDigits(double positive)
    {
        // "R" is the shortest round-trippable representation (closest to the exact value) since .NET Core 3.0.
        string text = positive.ToString("R", CultureInfo.InvariantCulture);
        int exponentIndex = text.IndexOfAny(['E', 'e']);
        int exponent = 0;
        string mantissa = text;
        if (exponentIndex >= 0)
        {
            exponent = int.Parse(text.AsSpan(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            mantissa = text[..exponentIndex];
        }

        int dot = mantissa.IndexOf('.', StringComparison.Ordinal);
        string allDigits = dot >= 0 ? mantissa.Remove(dot, 1) : mantissa;
        int pointPosition = (dot >= 0 ? dot : mantissa.Length) + exponent;
        string trimmedStart = allDigits.TrimStart('0');
        pointPosition -= allDigits.Length - trimmedStart.Length;
        return (trimmedStart.TrimEnd('0'), pointPosition);
    }

    /// <summary>Appends a string of digits grouped in threes from the right with <c>','</c>.</summary>
    private static void AppendGrouped(StringBuilder text, string integerDigits)
    {
        int firstGroup = integerDigits.Length % 3;
        if (firstGroup == 0)
        {
            firstGroup = 3;
        }

        text.Append(integerDigits, 0, firstGroup);
        for (int index = firstGroup; index < integerDigits.Length; index += 3)
        {
            text.Append(',').Append(integerDigits, index, 3);
        }
    }
}
