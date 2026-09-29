using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>
/// Everything the per-tier planner decided for one tier (the Python per-tier <c>dict</c> built in <c>run_tiers</c>).
/// Every dictionary enumerates in the same insertion order as the Python dict it ports.
/// </summary>
/// <param name="Tier">The tier number.</param>
/// <param name="NewItems">Items first automated in this tier, sorted ordinally by id (Python <c>new_items</c>).</param>
/// <param name="NewMachines">Recipe id to the number of machines built this tier (Python <c>new</c>), in run order.</param>
/// <param name="Recipes">Recipe id to recipe for every machine group running after this tier (Python <c>recs</c>).</param>
/// <param name="Runs">Recipe id to the exact machine count needed after this tier (Python <c>runs</c>).</param>
/// <param name="ExistingBefore">Recipe id to the machines already built before this tier (Python <c>existing</c>).</param>
/// <param name="Excess">New item to its excess output rate per minute (one machine at 100% clock).</param>
/// <param name="ExcessRecipe">New item to the recipe of its excess machine.</param>
/// <param name="Reused">Item to the machines' worth of spare capacity from earlier tiers put to work this tier.</param>
/// <param name="RawInput">Raw resource to its extraction rate per minute for the whole bus after this tier.</param>
/// <param name="Sink">Item to the rate per minute sent to the AWESOME Sink.</param>
/// <param name="Disposal">Fluid to its leftover rate per minute and the recipe that turns it into something sinkable.</param>
public sealed record TierResult(
    int Tier,
    IReadOnlyList<ItemId> NewItems,
    IReadOnlyDictionary<RecipeId, int> NewMachines,
    IReadOnlyDictionary<RecipeId, Recipe> Recipes,
    IReadOnlyDictionary<RecipeId, double> Runs,
    IReadOnlyDictionary<RecipeId, int> ExistingBefore,
    IReadOnlyDictionary<ItemId, double> Excess,
    IReadOnlyDictionary<ItemId, Recipe> ExcessRecipe,
    IReadOnlyDictionary<ItemId, double> Reused,
    IReadOnlyDictionary<ItemId, double> RawInput,
    IReadOnlyDictionary<ItemId, double> Sink,
    IReadOnlyDictionary<ItemId, (double Rate, Recipe Recipe)> Disposal);
