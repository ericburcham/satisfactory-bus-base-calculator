using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Excess;

/// <summary>Strategy for how much spare output (excess) a target item gets.</summary>
public interface IExcessPolicy
{
    /// <summary>The excess rate of a target item made with a recipe.</summary>
    /// <param name="item">The target item.</param>
    /// <param name="recipe">The recipe chosen for it.</param>
    /// <returns>The excess in items (or m3) per minute.</returns>
    double ExcessRate(ItemId item, Recipe recipe);
}
