using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Satisfactory.Planner.Core.Numerics;

namespace Satisfactory.Planner.Reporting.Formatting;

/// <summary>
/// Facade over <see cref="PythonNumerics"/> for the Python format specs the report writers use, such as
/// <c>{x:8.2f}</c>, <c>{x:10,.2f}</c>, <c>{n:4d}</c> and <c>str(round(x, 1))</c>.
/// </summary>
/// <remarks>
/// A width right-aligns the text with spaces (Python's default alignment for numbers) and never truncates.
/// </remarks>
public static class PythonNumberFormat
{
    /// <summary>Python <c>f"{value:{width}.{decimals}f}"</c>.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="decimals">The number of decimals (0 or more).</param>
    /// <param name="width">The minimum width; shorter text is padded with spaces on the left. 0 means no padding.</param>
    /// <returns>The formatted value.</returns>
    public static string Fixed(double value, int decimals, int width = 0) =>
        PythonTextFormat.PadLeft(PythonNumerics.FormatFixed(value, decimals, thousands: false), width);

    /// <summary>Python <c>f"{value:{width},.{decimals}f}"</c> (thousands separator <c>','</c>).</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="decimals">The number of decimals (0 or more).</param>
    /// <param name="width">The minimum width; shorter text is padded with spaces on the left. 0 means no padding.</param>
    /// <returns>The formatted value.</returns>
    public static string FixedThousands(double value, int decimals, int width = 0) =>
        PythonTextFormat.PadLeft(PythonNumerics.FormatFixed(value, decimals, thousands: true), width);

    /// <summary>Python <c>f"{value:{width}d}"</c>.</summary>
    /// <param name="value">The integer to format.</param>
    /// <param name="width">The minimum width; shorter text is padded with spaces on the left. 0 means no padding.</param>
    /// <returns>The formatted integer.</returns>
    [SuppressMessage(
        "Naming",
        "CA1720:Identifier contains type name",
        Justification = "The name is fixed by the shared port spec (PythonNumberFormat.Integer mirrors Python's 'd' spec).")]
    public static string Integer(long value, int width = 0) =>
        PythonTextFormat.PadLeft(value.ToString(CultureInfo.InvariantCulture), width);

    /// <summary>Python <c>str(round(value, digits))</c> for a float.</summary>
    /// <param name="value">The float to round.</param>
    /// <param name="digits">The number of decimals (0 or more).</param>
    /// <returns>The Python representation of the rounded float, e.g. <c>16.5</c>, <c>1000.0</c>, <c>-0.0</c>.</returns>
    public static string RoundRepr(double value, int digits) => PythonNumerics.Repr(PythonNumerics.Round(value, digits));

    /// <summary>
    /// Python <c>str(round(value, digits))</c> where <paramref name="isInteger"/> says whether the Python value is an
    /// <c>int</c>: <c>round(int, n)</c> returns the int unchanged and <c>str</c> prints it without a decimal point
    /// (<c>-30000</c>, <c>4</c>); otherwise the value is a float (see <see cref="RoundRepr(double, int)"/>).
    /// </summary>
    /// <param name="value">The value; must be a finite whole number when <paramref name="isInteger"/> is set.</param>
    /// <param name="digits">The number of decimals (0 or more).</param>
    /// <param name="isInteger">The Python value is an <c>int</c> rather than a <c>float</c>.</param>
    /// <returns>The Python representation of the rounded value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits"/> is negative, or <paramref name="isInteger"/> is set and the value is not a finite
    /// whole number.
    /// </exception>
    public static string RoundRepr(double value, int digits, bool isInteger)
    {
        if (!isInteger)
        {
            return RoundRepr(value, digits);
        }

        ArgumentOutOfRangeException.ThrowIfNegative(digits);
        if (!double.IsFinite(value) || Math.Floor(value) != value)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "An integer value must be a finite whole number.");
        }

        return new BigInteger(value).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Python <c>repr(value)</c> / <c>str(value)</c> of a float.</summary>
    /// <param name="value">The float to format.</param>
    /// <returns>The Python representation, e.g. <c>16.0</c>, <c>1e-05</c>, <c>inf</c>.</returns>
    public static string Repr(double value) => PythonNumerics.Repr(value);
}
