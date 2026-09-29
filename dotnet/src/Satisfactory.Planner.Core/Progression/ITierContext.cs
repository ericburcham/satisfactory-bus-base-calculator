using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>What is available in one progression tier (the Python planner's state after <c>set_tier(t)</c>).</summary>
public interface ITierContext
{
    /// <summary>The tier.</summary>
    int Tier { get; }

    /// <summary>The game data.</summary>
    GameData Data { get; }

    /// <summary>The recipes unlocked by this tier, in <see cref="GameData.Recipes"/> order (Python <c>self.recipes</c>).</summary>
    IReadOnlyList<Recipe> AvailableRecipes { get; }

    /// <summary>The natural resources whose extractor is not unlocked yet (Python <c>locked_raw</c>).</summary>
    IReadOnlySet<ItemId> LockedRawResources { get; }

    /// <summary>
    /// The items that can be made from extractable resources with the available recipes (Python <c>automatable</c>).
    /// </summary>
    IReadOnlySet<ItemId> Automatable { get; }

    /// <summary>The extractors of this tier (locked resources have none).</summary>
    IExtractorCatalog Extractors { get; }
}
