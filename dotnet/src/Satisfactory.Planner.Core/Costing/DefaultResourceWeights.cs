using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>
/// The Python weights (<c>RESOURCE_WEIGHT.get(c, 1.0)</c>): water is nearly free
/// (<see cref="PlannerConstants.ResourceWeights"/>), every other resource counts
/// <see cref="PlannerConstants.DefaultResourceWeight"/> per unit.
/// </summary>
public sealed class DefaultResourceWeights : IResourceWeights
{
    /// <inheritdoc />
    public double WeightOf(ItemId resource) =>
        PlannerConstants.ResourceWeights.TryGetValue(resource, out double weight)
            ? weight
            : PlannerConstants.DefaultResourceWeight;
}
