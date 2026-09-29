using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>How much one unit of a natural resource counts in the resource cost of items.</summary>
public interface IResourceWeights
{
    /// <summary>The weight of one unit (or m3) of an extractable resource.</summary>
    /// <param name="resource">The resource.</param>
    /// <returns>The weight.</returns>
    double WeightOf(ItemId resource);
}
