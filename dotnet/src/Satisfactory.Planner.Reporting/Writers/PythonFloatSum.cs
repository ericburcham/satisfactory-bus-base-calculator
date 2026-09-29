namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Python 3.12+ <c>sum()</c> of floats with the default start <c>0</c>. Since Python 3.12, <c>sum</c> of floats is not
/// a plain left-to-right addition: it adds with Neumaier's compensated (improved Kahan-Babuska) summation, so for
/// example <c>sum([0.1, 0.2, 0.3])</c> is <c>0.6</c> while <c>0.1 + 0.2 + 0.3</c> is <c>0.6000000000000001</c>.
/// </summary>
/// <remarks>
/// Port of CPython's <c>builtin_sum_impl</c> float path: the first item is added to the integer start (<c>0 + x</c>);
/// every later item <c>x</c> is added as <c>t = hi + x</c> with the lost low-order part accumulated in <c>lo</c>
/// (<c>(hi - t) + x</c> when <c>|hi| &gt;= |x|</c>, else <c>(x - t) + hi</c>); the result is <c>hi + lo</c> when
/// <c>lo</c> is non-zero and finite, else <c>hi</c>. An empty sequence sums to <c>0</c>.
/// </remarks>
internal static class PythonFloatSum
{
    public static double Of(IEnumerable<double> values)
    {
        double hi = 0.0;
        double lo = 0.0;
        bool first = true;
        foreach (double x in values)
        {
            if (first)
            {
                // int 0 + float: the float path starts from the first item's value (0.0 + x, which turns -0.0 into 0.0).
                hi = 0.0 + x;
                first = false;
                continue;
            }

            double t = hi + x;
            if (Math.Abs(hi) >= Math.Abs(x))
            {
                lo += (hi - t) + x;
            }
            else
            {
                lo += (x - t) + hi;
            }

            hi = t;
        }

        return lo != 0 && double.IsFinite(lo) ? hi + lo : hi;
    }
}
