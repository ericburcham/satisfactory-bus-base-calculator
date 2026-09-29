using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>Test double for <see cref="ITierReportWriter"/>: logs <c>{name}:{tier}</c> and records every call.</summary>
public sealed class FakeTierReportWriter(string name, List<string> log) : ITierReportWriter
{
    private readonly List<(TierResult Result, TierReportContext Context)> _calls = [];

    public IReadOnlyList<(TierResult Result, TierReportContext Context)> Calls => _calls;

    public void Write(TierResult result, TierReportContext context)
    {
        log.Add($"{name}:{result.Tier}");
        _calls.Add((result, context));
    }
}
