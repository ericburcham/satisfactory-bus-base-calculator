using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Sizes every machine and extractor needed to produce a set of excess rates.</summary>
public interface IProductionSolver
{
    /// <summary>Solves the production for the given excess targets.</summary>
    /// <param name="excess">Item to its required excess rate per minute (insertion order is the demand order).</param>
    /// <param name="context">The recipes, credit, existing capacity, extractors and disposal finder.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="UndisposableFluidException">A leftover fluid cannot be disposed of.</exception>
    /// <exception cref="PlannerException">The production cannot be solved (the Python <c>sys.exit</c> cases).</exception>
    ProductionPlan Solve(IReadOnlyDictionary<ItemId, double> excess, SolveContext context);
}
