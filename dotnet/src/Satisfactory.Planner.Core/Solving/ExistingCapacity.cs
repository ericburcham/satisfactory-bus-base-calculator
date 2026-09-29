using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>The machines built so far; the per-tier driver adds each tier's new machines.</summary>
public sealed class ExistingCapacity : IExistingCapacity
{
    private readonly OrderedDictionary<RecipeId, int> _machines = new();
    private readonly Dictionary<RecipeId, Recipe> _recipes = [];

    /// <inheritdoc />
    public IReadOnlyList<(RecipeId Id, Recipe Recipe, int Machines)> Machines =>
        _machines.Select(entry => (entry.Key, _recipes[entry.Key], entry.Value)).ToList().AsReadOnly();

    /// <inheritdoc />
    public int MachinesOf(RecipeId id) => _machines.GetValueOrDefault(id, 0);

    /// <summary>
    /// Adds built machines: <c>existing[rc] = existing.get(rc, 0) + k</c> (a recipe keeps its first position) and
    /// <c>existing_rec[rc] = recipe</c>.
    /// </summary>
    /// <param name="recipe">The recipe the machines run.</param>
    /// <param name="machines">The number of machines built.</param>
    public void Add(Recipe recipe, int machines)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        _machines[recipe.Id] = _machines.GetValueOrDefault(recipe.Id, 0) + machines;
        _recipes[recipe.Id] = recipe;
    }

    /// <summary>An ordered copy of the machine counts (Python <c>dict(p.existing)</c>).</summary>
    /// <returns>Recipe id to machines, in the order they were first built.</returns>
    public IReadOnlyDictionary<RecipeId, int> Snapshot() => new OrderedDictionary<RecipeId, int>(_machines);
}
