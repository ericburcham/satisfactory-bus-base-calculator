using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Python <c>write_tier</c> lines 826-836: in build order, <c>total_new += k</c>, <c>w = power(r, k)</c>, then
/// <c>gen -= w</c> when <c>w</c> is negative and <c>mw += w</c> otherwise (both start at <c>0.0</c>).
/// </summary>
public sealed class TierTotalsCalculator : ITierTotalsCalculator
{
    private readonly IPowerCalculator _power;

    /// <summary>Creates the calculator.</summary>
    /// <param name="power">Computes the power of each machine group.</param>
    public TierTotalsCalculator(IPowerCalculator power)
    {
        ArgumentNullException.ThrowIfNull(power);
        _power = power;
    }

    /// <inheritdoc />
    public TierTotals Calculate(IReadOnlyList<MachineGroupNode> buildOrder)
    {
        ArgumentNullException.ThrowIfNull(buildOrder);
        int totalNew = 0;
        double used = 0.0;
        double generated = 0.0;
        foreach (MachineGroupNode machine in buildOrder)
        {
            totalNew += machine.NewMachines;
            double powerMw = _power.PowerMw(machine.Recipe, machine.NewMachines);
            if (powerMw < 0)
            {
                generated -= powerMw;
            }
            else
            {
                used += powerMw;
            }
        }

        return new TierTotals(totalNew, used, generated);
    }
}
