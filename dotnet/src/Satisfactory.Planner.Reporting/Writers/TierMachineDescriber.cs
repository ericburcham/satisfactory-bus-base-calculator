using System.Globalization;
using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// The per-machine texts shared by the text and CSV build-order reports (Python <c>write_tier</c>'s <c>maker</c>,
/// <c>src</c>, <c>byp</c> and <c>disposal_by_recipe</c>).
/// </summary>
internal sealed class TierMachineDescriber
{
    private readonly GameData _data;
    private readonly DirectedAcyclicGraph<TierGraphNode> _graph;
    private readonly Dictionary<ItemId, MachineGroupNode> _makers = [];
    private readonly Dictionary<RecipeId, (ItemId Fluid, double Rate)> _disposalByRecipe = [];

    public TierMachineDescriber(TierResult result, TierReportContext context)
    {
        _data = context.Data;
        _graph = context.Graph;

        // maker: the first machine group (graph insertion = NewMachines order) per main product (Python setdefault).
        foreach (TierGraphNode node in _graph.Nodes)
        {
            if (node is MachineGroupNode machine)
            {
                _makers.TryAdd(machine.Recipe.MainProduct.Item, machine);
            }
        }

        // disposal_by_recipe = {r.className: (f, q) for f, (q, r) in disposal.items()}: a later fluid wins.
        foreach (KeyValuePair<ItemId, (double Rate, Recipe Recipe)> disposal in result.Disposal)
        {
            _disposalByRecipe[disposal.Value.Recipe.Id] = (disposal.Key, disposal.Value.Rate);
        }
    }

    /// <summary>
    /// Python <c>src(item)</c>: <c>"{name} (#{step})"</c> when a new machine group of this tier makes the item, the
    /// plain name for a raw resource, otherwise <c>"{name} (bus)"</c>.
    /// </summary>
    public string SourceOf(ItemId item)
    {
        string name = _data.ItemName(item);
        if (_makers.TryGetValue(item, out MachineGroupNode? maker))
        {
            return string.Create(CultureInfo.InvariantCulture, $"{name} (#{maker.Step})");
        }

        if (_graph.TryGetNode(ExternalInputNode.IdFor(item), out TierGraphNode? input) && input is RawResourceInputNode)
        {
            return name;
        }

        return name + " (bus)";
    }

    /// <summary>The sources of a recipe's ingredients, in ingredient order.</summary>
    public IReadOnlyList<string> IngredientSources(Recipe recipe) =>
        recipe.Ingredients.Select(i => SourceOf(i.Item)).ToList();

    /// <summary>The names of a recipe's byproducts (Python <c>products[1:]</c>), in product order.</summary>
    public IReadOnlyList<string> ByproductNames(Recipe recipe) =>
        recipe.Products.Skip(1).Select(p => _data.ItemName(p.Item)).ToList();

    /// <summary>
    /// The disposal note of a recipe that turns a leftover fluid into something sinkable, or <see langword="null"/>.
    /// </summary>
    public string? DisposalNote(RecipeId recipe)
    {
        if (!_disposalByRecipe.TryGetValue(recipe, out (ItemId Fluid, double Rate) disposal))
        {
            return null;
        }

        string rate = PythonNumerics.FormatFixed(disposal.Rate, 1, thousands: true);
        return $"turns leftover {_data.ItemName(disposal.Fluid)} ({rate}/min) into something sinkable";
    }
}
