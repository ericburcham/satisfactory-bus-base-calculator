using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>
/// Python <c>run_tiers</c>: walks the tiers in order. Every item that first becomes automatable in a tier gets one
/// excess machine; the excess of earlier tiers stays required; machines built in earlier tiers stay built and their
/// spare capacity is used before new machines are built.
/// </summary>
public sealed class TierPlanner : ITierPlanner
{
    private readonly ITierContextFactory _contexts;
    private readonly IResourceCostCalculator _costs;
    private readonly IRecipeSelector _selector;
    private readonly IPlanOptimizer _optimizer;

    /// <summary>Creates the planner.</summary>
    /// <param name="contexts">Creates each tier's context.</param>
    /// <param name="costs">Computes resource costs (for the new-item check before optimising).</param>
    /// <param name="selector">Chooses recipes (for the new-item check before optimising).</param>
    /// <param name="optimizer">Plans each tier.</param>
    public TierPlanner(
        ITierContextFactory contexts, IResourceCostCalculator costs, IRecipeSelector selector, IPlanOptimizer optimizer)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentNullException.ThrowIfNull(costs);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(optimizer);
        _contexts = contexts;
        _costs = costs;
        _selector = selector;
        _optimizer = optimizer;
    }

    /// <inheritdoc />
    public IReadOnlyList<TierResult> PlanAllTiers(PlannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var results = new List<TierResult>();
        var existing = new ExistingCapacity();
        var fixedExcess = new OrderedDictionary<ItemId, double>();
        var fixedExcessRecipes = new OrderedDictionary<ItemId, Recipe>();
        var before = new HashSet<ItemId>();
        IReadOnlyDictionary<RecipeId, double> previousRuns = new OrderedDictionary<RecipeId, double>();
        for (int tier = PlannerConstants.FirstTier; tier <= PlannerConstants.LastTier; tier++)
        {
            ITierContext context = _contexts.Create(tier);
            GameData data = context.Data;

            // Python p.recipe_for(c) before optimise: a fresh cost table and choice without any credit restrictions.
            var credit = new CreditPolicy();
            RecipeChoice initialChoice = _selector.Choose(context, _costs.Calculate(context, credit), credit);
            var newItems = context.Automatable
                .Where(c => !before.Contains(c)
                    && !data.IsResource(c)
                    && !PlannerConstants.Unsinkable.Contains(c)
                    && (options.IncludeIngots || !c.Value.Contains(PlannerConstants.IngotMarker, StringComparison.Ordinal))
                    && initialChoice.For(c) is not null)
                .Order()
                .ToList()
                .AsReadOnly();
            var targets = fixedExcess.Keys.Union(newItems).Order().ToList().AsReadOnly();

            ProductionPlan plan = _optimizer.Optimize(targets, fixedExcess, fixedExcessRecipes, context, existing);

            var runs = new OrderedDictionary<RecipeId, double>();
            var recipes = new OrderedDictionary<RecipeId, Recipe>();
            foreach (MachineRow row in plan.Machines)
            {
                runs[row.Recipe.Id] = row.Count;
                recipes[row.Recipe.Id] = row.Recipe;
            }

            var newMachines = new OrderedDictionary<RecipeId, int>();
            foreach ((RecipeId id, double count) in runs)
            {
                int k = (int)Math.Ceiling(count - existing.MachinesOf(id) - PlannerConstants.NewMachineTolerance);
                if (k > 0)
                {
                    newMachines[id] = k;
                }
            }

            var reused = new OrderedDictionary<ItemId, double>();
            foreach ((RecipeId id, double count) in runs)
            {
                double gain = PythonNumerics.Min(count, existing.MachinesOf(id)) - previousRuns.GetValueOrDefault(id, 0.0);
                if (gain > PlannerConstants.ReusedGainTolerance)
                {
                    ItemId product = recipes[id].MainProduct.Item;
                    reused[product] = reused.GetValueOrDefault(product, 0.0) + gain;
                }
            }

            var raw = new OrderedDictionary<ItemId, double>();
            foreach (ExtractorRow row in plan.Extractors)
            {
                raw[row.Resource] = row.RatePerMinute;
            }

            var excess = new OrderedDictionary<ItemId, double>();
            var excessRecipe = new OrderedDictionary<ItemId, Recipe>();
            foreach (ItemId item in newItems)
            {
                excess[item] = plan.Excess[item];
                excessRecipe[item] = plan.ExcessRecipes[item];
            }

            results.Add(new TierResult(
                tier, newItems, newMachines, recipes, runs, existing.Snapshot(), excess, excessRecipe, reused, raw,
                plan.Sink, plan.Disposal));

            // Lock this tier in: machines stay built, excess targets stay required.
            foreach ((RecipeId id, int k) in newMachines)
            {
                existing.Add(recipes[id], k);
            }

            foreach (ItemId item in newItems)
            {
                fixedExcess[item] = plan.Excess[item];
                fixedExcessRecipes[item] = plan.ExcessRecipes[item];
            }

            previousRuns = runs;
            before.UnionWith(context.Automatable);
        }

        return results.AsReadOnly();
    }
}
