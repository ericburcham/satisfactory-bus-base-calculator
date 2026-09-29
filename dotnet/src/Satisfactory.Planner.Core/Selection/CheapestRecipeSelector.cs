using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;

namespace Satisfactory.Planner.Core.Selection;

/// <summary>
/// Python <c>_candidates</c> and <c>_choose_all</c>: every automatable, non-extractable item gets its most preferred
/// candidate recipe (<see cref="RecipeCandidateComparer"/>) that does not make the item depend on itself through the
/// other chosen recipes (for example Compacted Coal via Rocket Fuel, which needs Compacted Coal).
/// </summary>
public sealed class CheapestRecipeSelector : IRecipeSelector
{
    /// <inheritdoc />
    public RecipeChoice Choose(ITierContext context, ResourceCostTable costs, CreditPolicy credit)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(credit);
        IExtractorCatalog extractors = context.Extractors;

        // Python iterates a set here; only "sorted(pick)" order is observed, so sort up front.
        var items = context.Automatable.Where(c => !extractors.IsExtractable(c)).Order().ToList();
        var candidates = new Dictionary<ItemId, IReadOnlyList<Recipe>>();
        var pick = new OrderedDictionary<ItemId, int>();
        foreach (ItemId item in items)
        {
            IReadOnlyList<Recipe> list = Candidates(context, costs, credit, item);
            candidates[item] = list;
            if (list.Count > 0)
            {
                pick[item] = 0;
            }
        }

        var cycles = new CycleCheck(extractors, candidates, pick);
        var sortedPicks = pick.Keys.Order().ToList();
        for (int pass = 0; pass < PlannerConstants.CycleRejectionPasses; pass++)
        {
            bool changed = false;
            foreach (ItemId item in sortedPicks)
            {
                while (cycles.IsCyclic(item) && pick[item] + 1 < candidates[item].Count)
                {
                    pick[item] += 1;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        var chosen = new OrderedDictionary<ItemId, Recipe>();
        foreach ((ItemId item, int index) in pick)
        {
            chosen[item] = candidates[item][index];
        }

        return new RecipeChoice(chosen);
    }

    /// <summary>
    /// The candidate recipes for an item, most preferred first (Python <c>_candidates</c>): available recipes that
    /// make the item from automatable ingredients, do not make a forbidden byproduct other than the item itself and
    /// are not unpackaging recipes, stably sorted with <see cref="RecipeCandidateComparer"/>.
    /// </summary>
    /// <param name="context">The tier.</param>
    /// <param name="costs">The tier's resource costs.</param>
    /// <param name="credit">The credit policy.</param>
    /// <param name="item">The item.</param>
    /// <returns>The candidates; empty when the item cannot be made.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Instance member by the shared port spec: part of the selector strategy's public surface.")]
    public IReadOnlyList<Recipe> Candidates(ITierContext context, ResourceCostTable costs, CreditPolicy credit, ItemId item)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(credit);
        IReadOnlySet<ItemId> automatable = context.Automatable;
        var comparer = new RecipeCandidateComparer(item, context.Data.ItemName(item), costs);
        return context.AvailableRecipes
            .Where(r => r.Produces(item)
                && r.Ingredients.All(i => automatable.Contains(i.Item))
                && !r.Products.Any(p => credit.IsForbiddenByproduct(p.Item) && p.Item != item)
                && !r.Name.StartsWith(PlannerConstants.UnpackagePrefix, StringComparison.Ordinal))
            .OrderBy(r => r, comparer)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>The <c>ingredients</c> / <c>cyclic</c> closures of Python <c>_choose_all</c> over the current picks.</summary>
    private sealed class CycleCheck(
        IExtractorCatalog extractors,
        Dictionary<ItemId, IReadOnlyList<Recipe>> candidates,
        OrderedDictionary<ItemId, int> pick)
    {
        public bool IsCyclic(ItemId item)
        {
            var seen = new HashSet<ItemId>();
            var stack = new List<ItemId>(Ingredients(item));
            while (stack.Count > 0)
            {
                ItemId next = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                if (next == item)
                {
                    return true;
                }

                if (!seen.Add(next))
                {
                    continue;
                }

                stack.AddRange(Ingredients(next));
            }

            return false;
        }

        private IEnumerable<ItemId> Ingredients(ItemId item)
        {
            if (extractors.IsExtractable(item) || !pick.TryGetValue(item, out int index))
            {
                return [];
            }

            return candidates[item][index].Ingredients.Select(i => i.Item);
        }
    }
}
