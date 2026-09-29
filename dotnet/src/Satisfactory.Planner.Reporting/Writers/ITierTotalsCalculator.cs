using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>Sums a tier's new machines and their power.</summary>
public interface ITierTotalsCalculator
{
    /// <summary>Sums the machine groups of a tier, in build order.</summary>
    /// <param name="buildOrder">The tier's machine groups in build order.</param>
    /// <returns>The totals.</returns>
    TierTotals Calculate(IReadOnlyList<MachineGroupNode> buildOrder);
}
