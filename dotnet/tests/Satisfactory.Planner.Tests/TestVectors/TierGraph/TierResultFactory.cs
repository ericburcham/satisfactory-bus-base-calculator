using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Tests.TestVectors.TierGraph;

/// <summary>
/// Builds <see cref="TierResult"/> instances by hand for the tier graph tests. Only the fields the graph reads are
/// filled (<see cref="TierResult.NewItems"/>, <see cref="TierResult.NewMachines"/>, <see cref="TierResult.Recipes"/>,
/// <see cref="TierResult.Sink"/>); every dictionary is an <see cref="OrderedDictionary{TKey, TValue}"/> so the
/// enumeration order is the order given.
/// </summary>
internal static class TierResultFactory
{
    /// <summary>Creates a tier result.</summary>
    /// <param name="data">The game data the recipes are looked up in (<see cref="GameData.Recipes"/>).</param>
    /// <param name="tier">The tier number.</param>
    /// <param name="newMachines">Recipe class name and new machine count, in <c>new</c> order.</param>
    /// <param name="newItems">The new items.</param>
    /// <param name="sink">Sunk items and rates, in order (none when omitted).</param>
    /// <param name="recipes">
    /// The <c>recs</c> recipe class names in order; when omitted, the <paramref name="newMachines"/> recipes.
    /// </param>
    /// <returns>The tier result.</returns>
    public static TierResult Create(
        GameData data,
        int tier,
        IEnumerable<(string Recipe, int Machines)> newMachines,
        IEnumerable<string> newItems,
        IEnumerable<(string Item, double Rate)>? sink = null,
        IEnumerable<string>? recipes = null)
    {
        var byId = data.Recipes.ToDictionary(r => r.Id);
        var machines = new OrderedDictionary<RecipeId, int>();
        foreach ((string recipe, int count) in newMachines)
        {
            machines.Add(new RecipeId(recipe), count);
        }

        var recs = new OrderedDictionary<RecipeId, Recipe>();
        foreach (RecipeId id in recipes?.Select(r => new RecipeId(r)) ?? machines.Keys)
        {
            recs.Add(id, byId[id]);
        }

        var sunk = new OrderedDictionary<ItemId, double>();
        foreach ((string item, double rate) in sink ?? [])
        {
            sunk.Add(new ItemId(item), rate);
        }

        return new TierResult(
            tier,
            newItems.Select(c => new ItemId(c)).ToList().AsReadOnly(),
            machines,
            recs,
            new OrderedDictionary<RecipeId, double>(),
            new OrderedDictionary<RecipeId, int>(),
            new OrderedDictionary<ItemId, double>(),
            new OrderedDictionary<ItemId, Recipe>(),
            new OrderedDictionary<ItemId, double>(),
            new OrderedDictionary<ItemId, double>(),
            sunk,
            new OrderedDictionary<ItemId, (double Rate, Recipe Recipe)>());
    }
}
