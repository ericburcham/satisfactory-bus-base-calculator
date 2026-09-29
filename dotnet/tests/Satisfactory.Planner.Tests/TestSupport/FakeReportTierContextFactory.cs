using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ITierContextFactory"/> used by the report tests: every created context gets its own
/// extractor catalog instance (kept in <see cref="ExtractorsByTier"/>) and each call is logged as <c>context:{tier}</c>.
/// </summary>
public sealed class FakeReportTierContextFactory(GameData data, List<string> log) : ITierContextFactory
{
    private readonly Dictionary<int, IExtractorCatalog> _extractors = [];

    /// <summary>The catalog handed out for each requested tier.</summary>
    public IReadOnlyDictionary<int, IExtractorCatalog> ExtractorsByTier => _extractors;

    public ITierContext Create(int tier)
    {
        log.Add($"context:{tier}");
        var extractors = new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, new HashSet<ItemId>());
        _extractors[tier] = extractors;
        return new FakeReportTierContext(tier, data, extractors);
    }
}
