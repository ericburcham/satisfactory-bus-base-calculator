using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Creates the fluid disposal finder of one optimisation round.</summary>
public interface IFluidDisposalFinderFactory
{
    /// <summary>Creates a finder for a tier and its current resource costs.</summary>
    /// <param name="context">The tier.</param>
    /// <param name="costs">The round's resource costs.</param>
    /// <returns>The finder.</returns>
    IFluidDisposalFinder Create(ITierContext context, ResourceCostTable costs);
}
