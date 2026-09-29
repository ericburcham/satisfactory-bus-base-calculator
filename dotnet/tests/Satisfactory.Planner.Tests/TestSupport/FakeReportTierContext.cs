using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>Test double for <see cref="ITierContext"/> used by the report tests: only the tier and extractors matter.</summary>
public sealed class FakeReportTierContext(int tier, GameData data, IExtractorCatalog extractors) : ITierContext
{
    public int Tier { get; } = tier;

    public GameData Data { get; } = data;

    public IReadOnlyList<Recipe> AvailableRecipes { get; } = [];

    public IReadOnlySet<ItemId> LockedRawResources { get; } = new HashSet<ItemId>();

    public IReadOnlySet<ItemId> Automatable { get; } = new HashSet<ItemId>();

    public IExtractorCatalog Extractors { get; } = extractors;
}
