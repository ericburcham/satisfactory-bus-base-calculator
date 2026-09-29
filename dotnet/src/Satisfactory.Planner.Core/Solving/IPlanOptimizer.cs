using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Chooses recipes and plans a tier's production, repeating until the choices are consistent.</summary>
public interface IPlanOptimizer
{
    /// <summary>Plans the production of a tier (Python <c>optimise</c>).</summary>
    /// <param name="targets">The excess targets, sorted; targets already in <paramref name="fixedExcess"/> keep it.</param>
    /// <param name="fixedExcess">The excess fixed by earlier tiers, in the order it was fixed.</param>
    /// <param name="fixedExcessRecipes">The recipes of the fixed excess machines.</param>
    /// <param name="context">The tier.</param>
    /// <param name="existing">The machines built in earlier tiers.</param>
    /// <returns>The plan, with <see cref="ProductionPlan.ExcessRecipes"/> set.</returns>
    /// <exception cref="PlannerException">The tier cannot be planned.</exception>
    ProductionPlan Optimize(
        IReadOnlyList<ItemId> targets,
        IReadOnlyDictionary<ItemId, double> fixedExcess,
        IReadOnlyDictionary<ItemId, Recipe> fixedExcessRecipes,
        ITierContext context,
        IExistingCapacity existing);
}
