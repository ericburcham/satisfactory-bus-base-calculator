using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ITierDotWriter"/>: returns <c>{folder}/fake_tier_{tier}.dot</c>, logs <c>dot:{tier}</c>
/// and records every call.
/// </summary>
public sealed class FakeTierDotWriter(List<string> log) : ITierDotWriter
{
    private readonly List<(TierResult Result, TierReportContext Context)> _calls = [];

    public IReadOnlyList<(TierResult Result, TierReportContext Context)> Calls => _calls;

    public string Write(TierResult result, TierReportContext context)
    {
        log.Add($"dot:{result.Tier}");
        _calls.Add((result, context));
        return $"{context.OutputFolder}/fake_tier_{result.Tier}.dot";
    }
}
