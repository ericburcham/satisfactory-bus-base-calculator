using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Finds the recipe that turns a leftover fluid into something sinkable.</summary>
public interface IFluidDisposalFinder
{
    /// <summary>The preferred disposal recipe of a fluid.</summary>
    /// <param name="fluid">The leftover fluid.</param>
    /// <returns>The recipe.</returns>
    /// <exception cref="UndisposableFluidException">No recipe can dispose of the fluid.</exception>
    Recipe Find(ItemId fluid);
}
