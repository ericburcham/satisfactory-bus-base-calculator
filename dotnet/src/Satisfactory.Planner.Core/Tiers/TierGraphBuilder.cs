using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>
/// Builds a tier's graph (Builder): the Python <c>maker</c> map of <c>tier_order</c> and the node and edge logic of the
/// <c>.dot</c> part of <c>write_tier</c>.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>One <see cref="MachineGroupNode"/> per <see cref="TierResult.NewMachines"/> entry, in that order, before any
/// edge.</item>
/// <item>The maker of an item is the first machine group (in that order) whose main product it is.</item>
/// <item>For each machine group in that order and each ingredient in recipe order: an edge from the ingredient's maker
/// or, when no new machine makes it, from its external input node <c>in_&lt;item&gt;</c> (a
/// <see cref="RawResourceInputNode"/> when the extractor catalog can extract it, else a <see cref="BusInputNode"/>;
/// created on first use). Every ingredient entry gets its own edge, so parallel edges are kept.</item>
/// <item>When the tier sinks anything: a <see cref="SinkNode"/> and, per sink item in order, a labelled edge from the
/// item's maker, else its existing input node, else a new <see cref="BusInputNode"/>.</item>
/// </list>
/// </remarks>
public sealed class TierGraphBuilder : ITierGraphBuilder
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A recipe of <see cref="TierResult.NewMachines"/> is missing from
    /// <see cref="TierResult.Recipes"/>.</exception>
    /// <exception cref="PlannerException">The machines' recipes feed each other in a cycle (or one consumes its own
    /// main product).</exception>
    public DirectedAcyclicGraph<TierGraphNode> Build(TierResult result, GameData data, IExtractorCatalog extractors)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(extractors);

        var graph = new DirectedAcyclicGraph<TierGraphNode>();
        var newItems = new HashSet<ItemId>(result.NewItems);
        var machines = new List<MachineGroupNode>(result.NewMachines.Count);
        var maker = new Dictionary<ItemId, MachineGroupNode>();

        foreach (KeyValuePair<RecipeId, int> entry in result.NewMachines)
        {
            Recipe recipe = result.Recipes.TryGetValue(entry.Key, out Recipe? found)
                ? found
                : throw new ArgumentException($"Tier {result.Tier} builds {entry.Key} but has no recipe for it.", nameof(result));
            ItemId product = recipe.MainProduct.Item;
            var machine = new MachineGroupNode(
                recipe, entry.Value, data.ItemName(product), BuildingName(recipe, data), newItems.Contains(product));
            graph.AddNode(machine);
            machines.Add(machine);
            maker.TryAdd(product, machine);
        }

        foreach (MachineGroupNode machine in machines)
        {
            foreach (ItemAmount ingredient in machine.Recipe.Ingredients)
            {
                TierGraphNode source = maker.TryGetValue(ingredient.Item, out MachineGroupNode? made)
                    ? made
                    : ExternalInput(graph, data, extractors, ingredient.Item);
                AddEdge(graph, result.Tier, source, machine, label: null);
            }
        }

        if (result.Sink.Count > 0)
        {
            var sink = new SinkNode();
            graph.AddNode(sink);
            foreach (KeyValuePair<ItemId, double> entry in result.Sink)
            {
                TierGraphNode source = maker.TryGetValue(entry.Key, out MachineGroupNode? made)
                    ? made
                    : SinkSource(graph, data, entry.Key);
                string label =
                    $"{data.ItemName(entry.Key)} {PythonNumerics.FormatFixed(entry.Value, 0, thousands: true)}/min";
                AddEdge(graph, result.Tier, source, sink, label);
            }
        }

        return graph;
    }

    /// <summary>Python <c>r.get("buildingName") or p.buildings[r["producedIn"][0]]["name"]</c>.</summary>
    private static string BuildingName(Recipe recipe, GameData data) =>
        recipe is GeneratorRecipe { BuildingName.Length: > 0 } generator
            ? generator.BuildingName
            : data.GetBuilding(recipe.BuildingId).Name;

    /// <summary>The ingredient's input node, created on first use: raw when extractable, else bus.</summary>
    private static TierGraphNode ExternalInput(
        DirectedAcyclicGraph<TierGraphNode> graph, GameData data, IExtractorCatalog extractors, ItemId item)
    {
        if (graph.TryGetNode(ExternalInputNode.IdFor(item), out TierGraphNode? existing))
        {
            return existing;
        }

        TierGraphNode created = extractors.IsExtractable(item)
            ? new RawResourceInputNode(item, data.ItemName(item))
            : new BusInputNode(item, data.ItemName(item));
        graph.AddNode(created);
        return created;
    }

    /// <summary>A sunk item nobody new makes: its existing input node, else a new bus input node (as the Python).</summary>
    private static TierGraphNode SinkSource(DirectedAcyclicGraph<TierGraphNode> graph, GameData data, ItemId item)
    {
        if (graph.TryGetNode(ExternalInputNode.IdFor(item), out TierGraphNode? existing))
        {
            return existing;
        }

        var created = new BusInputNode(item, data.ItemName(item));
        graph.AddNode(created);
        return created;
    }

    private static void AddEdge(
        DirectedAcyclicGraph<TierGraphNode> graph, int tier, TierGraphNode from, TierGraphNode to, string? label)
    {
        try
        {
            graph.AddEdge(from, to, label);
        }
        catch (CycleDetectedException e)
        {
            throw new PlannerException(
                $"Tier {tier}: recipe {to.Id} uses the output of recipe {from.Id}, which closes a cycle among the tier's new machines.",
                e);
        }
    }
}
