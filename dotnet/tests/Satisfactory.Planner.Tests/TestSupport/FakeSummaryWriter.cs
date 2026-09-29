using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ISummaryWriter"/>: logs <c>summary:{folder}</c>, records a copy of the tiers and returns
/// <see cref="Text"/>.
/// </summary>
public sealed class FakeSummaryWriter(List<string> log) : ISummaryWriter
{
    private readonly List<(IReadOnlyList<(TierResult Result, TierTotals Totals)> Tiers, string OutputFolder)> _calls = [];

    public string Text { get; init; } = "summary text\n";

    public IReadOnlyList<(IReadOnlyList<(TierResult Result, TierTotals Totals)> Tiers, string OutputFolder)> Calls => _calls;

    public string Write(IReadOnlyList<(TierResult Result, TierTotals Totals)> tiers, string outputFolder)
    {
        log.Add($"summary:{outputFolder}");
        _calls.Add((tiers.ToList(), outputFolder));
        return Text;
    }
}
