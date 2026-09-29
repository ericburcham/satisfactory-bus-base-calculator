using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Factory for <see cref="FluidDisposalFinder"/>.</summary>
public sealed class FluidDisposalFinderFactory : IFluidDisposalFinderFactory
{
    /// <inheritdoc />
    public IFluidDisposalFinder Create(ITierContext context, ResourceCostTable costs) =>
        new FluidDisposalFinder(context, costs);
}
