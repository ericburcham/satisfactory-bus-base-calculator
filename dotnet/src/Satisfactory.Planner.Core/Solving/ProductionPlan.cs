using System.Collections.ObjectModel;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>The result of a solve (Python <c>plan</c>'s rows, <c>excess</c>, <c>self.sink</c> and <c>self.disposal</c>).</summary>
/// <param name="Machines">Machine groups in Python <c>runs</c> order.</param>
/// <param name="Extractors">Extraction in Python <c>extract</c> order.</param>
/// <param name="Excess">Item to its excess rate per minute: the fixed excess, then the new targets.</param>
/// <param name="Sink">Item to the surplus per minute sent to the AWESOME Sink.</param>
/// <param name="Disposal">Fluid to the leftover rate turned into something sinkable, and the recipe that does it.</param>
public sealed record ProductionPlan(
    IReadOnlyList<MachineRow> Machines,
    IReadOnlyList<ExtractorRow> Extractors,
    IReadOnlyDictionary<ItemId, double> Excess,
    IReadOnlyDictionary<ItemId, double> Sink,
    IReadOnlyDictionary<ItemId, (double Rate, Recipe Recipe)> Disposal)
{
    /// <summary>
    /// Item to the recipe of its excess machine (Python <c>self.excess_recipe</c>); set by the optimiser, empty
    /// otherwise.
    /// </summary>
    public IReadOnlyDictionary<ItemId, Recipe> ExcessRecipes { get; init; } = ReadOnlyDictionary<ItemId, Recipe>.Empty;
}
