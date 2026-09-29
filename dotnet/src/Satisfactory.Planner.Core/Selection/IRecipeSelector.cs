using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Selection;

/// <summary>Strategy that picks one recipe for every item a tier can make.</summary>
public interface IRecipeSelector
{
    /// <summary>Chooses a recipe for every automatable, non-extractable item that has a candidate recipe.</summary>
    /// <param name="context">The tier.</param>
    /// <param name="costs">The tier's resource costs.</param>
    /// <param name="credit">The credit policy (its forbidden byproducts exclude candidates).</param>
    /// <returns>The choice.</returns>
    RecipeChoice Choose(ITierContext context, ResourceCostTable costs, CreditPolicy credit);
}
