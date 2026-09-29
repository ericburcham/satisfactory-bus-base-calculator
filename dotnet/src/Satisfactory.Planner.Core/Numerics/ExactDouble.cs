using System.Numerics;

namespace Satisfactory.Planner.Core.Numerics;

/// <summary>
/// The exact binary value of a finite double, <c>(-1)^IsNegative * Mantissa * 2^Exponent</c>, and exact decimal
/// rounding of it with <see cref="BigInteger"/> arithmetic.
/// </summary>
internal readonly record struct ExactDouble(bool IsNegative, ulong Mantissa, int Exponent)
{
    private const int MantissaBits = 52;
    private const ulong MantissaMask = (1UL << MantissaBits) - 1;
    private const int ExponentMask = 0x7FF;
    private const int ExponentBias = 1075;

    /// <summary>Decomposes a finite double (including signed zeros and subnormals).</summary>
    public static ExactDouble Of(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be finite.");
        }

        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        bool negative = (bits >> 63) != 0;
        int biased = (int)((bits >> MantissaBits) & ExponentMask);
        ulong fraction = bits & MantissaMask;
        return biased == 0
            ? new ExactDouble(negative, fraction, 1 - ExponentBias)
            : new ExactDouble(negative, fraction | (1UL << MantissaBits), biased - ExponentBias);
    }

    /// <summary>
    /// Returns <c>|value| * 10^decimals</c> rounded to an integer, half-to-even on the exact value.
    /// </summary>
    public BigInteger ScaledMagnitudeHalfEven(int decimals)
    {
        var scale = BigInteger.Pow(10, decimals);
        if (Exponent >= 0)
        {
            return (new BigInteger(Mantissa) << Exponent) * scale;
        }

        int shift = -Exponent;
        BigInteger numerator = new BigInteger(Mantissa) * scale;
        BigInteger quotient = numerator >> shift;
        BigInteger remainder = numerator - (quotient << shift);
        BigInteger half = BigInteger.One << (shift - 1);
        int comparison = remainder.CompareTo(half);
        if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
        {
            quotient += BigInteger.One;
        }

        return quotient;
    }
}
