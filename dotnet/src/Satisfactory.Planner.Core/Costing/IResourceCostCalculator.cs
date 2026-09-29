using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Costing;

/// <summary>Computes the natural resources needed per unit of every item of a tier.</summary>
public interface IResourceCostCalculator
{
    /// <summary>Computes the cost table of a tier under a credit policy.</summary>
    /// <param name="context">The tier.</param>
    /// <param name="credit">Which byproducts earn credit; the table keeps a reference for <see cref="ResourceCostTable.CostOf"/>.</param>
    /// <returns>The cost table.</returns>
    ResourceCostTable Calculate(ITierContext context, CreditPolicy credit);
}
