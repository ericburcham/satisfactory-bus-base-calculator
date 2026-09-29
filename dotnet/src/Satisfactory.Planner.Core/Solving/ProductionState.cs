using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>
/// The mutable state of one solve (the Python locals of <c>plan</c>): machines per recipe, the recipes, extraction per
/// resource and the fixed demand. Every map keeps Python insertion order.
/// </summary>
public sealed class ProductionState
{
    /// <summary>Recipe id to the number of machines running it (Python <c>runs</c>, a <c>defaultdict(float)</c>).</summary>
    public OrderedDictionary<RecipeId, double> Runs { get; } = new();

    /// <summary>Recipe id to the recipe of every entry of <see cref="Runs"/> (Python <c>rec</c>).</summary>
    public OrderedDictionary<RecipeId, Recipe> Recipes { get; } = new();

    /// <summary>Resource to its extraction rate per minute (Python <c>extract</c>, a <c>defaultdict(float)</c>).</summary>
    public OrderedDictionary<ItemId, double> Extract { get; } = new();

    /// <summary>Item to its demanded rate per minute: the excess targets (Python <c>demand</c>).</summary>
    public OrderedDictionary<ItemId, double> Demand { get; } = new();
}
