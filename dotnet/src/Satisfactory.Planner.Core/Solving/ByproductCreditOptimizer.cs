using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>
/// Python <c>optimise</c> (with the targets part of <c>plan</c>): choose recipes, plan, and repeat (at most
/// <see cref="PlannerConstants.OptimiseRounds"/> rounds) while a byproduct that was credited as useful ends up sunk or
/// disposed of (it then earns no credit), or while a leftover fluid cannot be disposed of (recipes leaving it over are
/// then avoided).
/// </summary>
public sealed class ByproductCreditOptimizer : IPlanOptimizer
{
    private readonly IResourceCostCalculator _costs;
    private readonly IRecipeSelector _selector;
    private readonly IExcessPolicy _excess;
    private readonly IProductionSolver _solver;
    private readonly IFluidDisposalFinderFactory _disposal;

    /// <summary>Creates the optimiser.</summary>
    /// <param name="costs">Computes the resource costs each round.</param>
    /// <param name="selector">Chooses the recipes each round.</param>
    /// <param name="excess">The excess rate of each new target.</param>
    /// <param name="solver">Solves the production.</param>
    /// <param name="disposal">Creates the fluid disposal finder of each round.</param>
    public ByproductCreditOptimizer(
        IResourceCostCalculator costs,
        IRecipeSelector selector,
        IExcessPolicy excess,
        IProductionSolver solver,
        IFluidDisposalFinderFactory disposal)
    {
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(excess);
        ArgumentNullException.ThrowIfNull(solver);
        ArgumentNullException.ThrowIfNull(disposal);
        _costs = costs;
        _selector = selector;
        _excess = excess;
        _solver = solver;
        _disposal = disposal;
    }

    /// <inheritdoc />
    /// <exception cref="PlannerException">
    /// A new target has no recipe (<c>"{name} is not machine-producible"</c>), the solver fails, or every round hit an
    /// undisposable fluid.
    /// </exception>
    public ProductionPlan Optimize(
        IReadOnlyList<ItemId> targets,
        IReadOnlyDictionary<ItemId, double> fixedExcess,
        IReadOnlyDictionary<ItemId, Recipe> fixedExcessRecipes,
        ITierContext context,
        IExistingCapacity existing)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(fixedExcess);
        ArgumentNullException.ThrowIfNull(fixedExcessRecipes);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(existing);

        // Python set_tier resets no_credit and no_byproduct; optimise runs once per tier.
        var credit = new CreditPolicy();
        ProductionPlan? result = null;
        var undisposable = new List<ItemId>();
        for (int round = 0; round < PlannerConstants.OptimiseRounds; round++)
        {
            ResourceCostTable costs = _costs.Calculate(context, credit);
            RecipeChoice choice = _selector.Choose(context, costs, credit);
            var excess = new OrderedDictionary<ItemId, double>(fixedExcess);
            var excessRecipes = new OrderedDictionary<ItemId, Recipe>(fixedExcessRecipes);
            foreach (ItemId target in targets)
            {
                if (excess.ContainsKey(target))
                {
                    continue;
                }

                Recipe recipe = choice.For(target)
                    ?? throw new PlannerException($"{context.Data.ItemName(target)} is not machine-producible");
                excess[target] = _excess.ExcessRate(target, recipe);
                excessRecipes[target] = recipe;
            }

            ProductionPlan plan;
            try
            {
                var solveContext = new SolveContext(
                    context, choice, credit, existing, context.Extractors, _disposal.Create(context, costs));
                plan = _solver.Solve(excess, solveContext);
            }
            catch (UndisposableFluidException e)
            {
                credit.ForbidByproduct(e.Fluid);
                undisposable.Add(e.Fluid);
                continue;
            }

            result = plan with { ExcessRecipes = excessRecipes };
            var wasted = plan.Sink.Keys.Concat(plan.Disposal.Keys).Where(credit.EarnsCredit).ToList();
            if (wasted.Count == 0)
            {
                return result;
            }

            credit.DenyCredit(wasted);
        }

        return result ?? throw new PlannerException(
            "No plan found: leftover fluids cannot be turned into something sinkable: "
            + string.Join(", ", undisposable.Distinct().Select(context.Data.ItemName)));
    }
}
