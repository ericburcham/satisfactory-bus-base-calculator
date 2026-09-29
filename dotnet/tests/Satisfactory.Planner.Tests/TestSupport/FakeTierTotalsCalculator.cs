using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ITierTotalsCalculator"/>: the n-th call (from 1) returns <c>TierTotals(n, 10n, 100n)</c>,
/// logs <c>totals</c> and records the build order it was given.
/// </summary>
public sealed class FakeTierTotalsCalculator(List<string> log) : ITierTotalsCalculator
{
    private readonly List<(IReadOnlyList<MachineGroupNode> BuildOrder, TierTotals Totals)> _calls = [];

    public IReadOnlyList<(IReadOnlyList<MachineGroupNode> BuildOrder, TierTotals Totals)> Calls => _calls;

    public TierTotals Calculate(IReadOnlyList<MachineGroupNode> buildOrder)
    {
        log.Add("totals");
        int n = _calls.Count + 1;
        var totals = new TierTotals(n, 10.0 * n, 100.0 * n);
        _calls.Add((buildOrder, totals));
        return totals;
    }
}
