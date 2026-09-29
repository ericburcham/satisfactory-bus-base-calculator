using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Machines built in earlier tiers (Python <c>existing</c> and <c>existing_rec</c>).</summary>
public interface IExistingCapacity
{
    /// <summary>Every recipe with built machines, in the order it was first built.</summary>
    IReadOnlyList<(RecipeId Id, Recipe Recipe, int Machines)> Machines { get; }

    /// <summary>The number of machines built for a recipe (Python <c>existing.get(rc, 0)</c>).</summary>
    /// <param name="id">The recipe id.</param>
    /// <returns>The number of machines; 0 when none.</returns>
    int MachinesOf(RecipeId id);
}
