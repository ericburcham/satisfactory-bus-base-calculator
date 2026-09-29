using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Null Object: no machines built yet.</summary>
public sealed class NoExistingCapacity : IExistingCapacity
{
    private NoExistingCapacity()
    {
    }

    /// <summary>The shared instance.</summary>
    public static NoExistingCapacity Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<(RecipeId Id, Recipe Recipe, int Machines)> Machines => [];

    /// <inheritdoc />
    public int MachinesOf(RecipeId id) => 0;
}
