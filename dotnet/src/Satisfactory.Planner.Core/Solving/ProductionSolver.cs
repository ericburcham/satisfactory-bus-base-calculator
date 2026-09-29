using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Power;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>
/// The non-optimising solve of Python <c>plan</c> (after the targets), <c>_converge</c> and <c>_trim</c>: each item is
/// made only by its chosen recipe, which runs exactly enough to cover the item's shortfall; spare capacity on earlier
/// machines and byproducts are used first; leftover fluids are turned into something sinkable; everything else left
/// over goes to the AWESOME Sink.
/// </summary>
public sealed class ProductionSolver : IProductionSolver
{
    private readonly IPowerCalculator _power;

    /// <summary>Creates the solver.</summary>
    /// <param name="power">Computes the power of each machine group.</param>
    public ProductionSolver(IPowerCalculator power)
    {
        ArgumentNullException.ThrowIfNull(power);
        _power = power;
    }

    /// <inheritdoc />
    public ProductionPlan Solve(IReadOnlyDictionary<ItemId, double> excess, SolveContext context)
    {
        ArgumentNullException.ThrowIfNull(excess);
        ArgumentNullException.ThrowIfNull(context);
        var run = new Run(context);
        var excessCopy = new OrderedDictionary<ItemId, double>();
        foreach ((ItemId item, double rate) in excess)
        {
            excessCopy[item] = rate;
            run.State.Demand[item] = rate;
        }

        // Python self.disposal: fluid -> m3/min turned into a sinkable solid; accumulates over the whole loop.
        var disposal = new OrderedDictionary<ItemId, double>();
        var disposalRecipes = new Dictionary<ItemId, Recipe>();
        bool settled = false;
        for (int iteration = 0; iteration < PlannerConstants.DisposalIterations; iteration++)
        {
            run.Converge();
            run.Trim(disposal.Keys);

            // Fluid byproducts can't be sunk: turn any leftover fluid into a solid, then settle the new demand.
            var left = ProductionBalance.Of(run.State)
                .Where(e => e.Value > PlannerConstants.LeftoverTolerance && context.Tier.Data.IsFluid(e.Key))
                .ToList();
            if (left.Count == 0)
            {
                settled = true;
                break;
            }

            foreach ((ItemId fluid, double q) in left)
            {
                Recipe recipe = context.DisposalFinder.Find(fluid);
                run.State.Recipes[recipe.Id] = recipe;
                ItemAmount use = recipe.Ingredients.First(i => i.Item == fluid);
                AddTo(run.State.Runs, recipe.Id, q / (use.Amount * 60 / recipe.TimeSeconds));
                AddTo(disposal, fluid, q);
                disposalRecipes[fluid] = recipe;
            }
        }

        if (!settled)
        {
            throw new PlannerException("Byproduct disposal did not converge");
        }

        var machines = new List<MachineRow>();
        foreach ((RecipeId id, double count) in run.State.Runs)
        {
            Recipe recipe = run.State.Recipes[id];
            machines.Add(new MachineRow(recipe, count, _power.PowerMw(recipe, count)));
        }

        var extractors = new List<ExtractorRow>();
        foreach ((ItemId resource, double rate) in run.State.Extract)
        {
            ExtractorSpec spec = context.Extractors.For(resource)
                ?? throw new InvalidOperationException($"{resource} was extracted but has no extractor.");
            extractors.Add(new ExtractorRow(resource, spec, rate));
        }

        var surplus = new OrderedDictionary<ItemId, double>();
        foreach ((ItemId item, double rate) in ProductionBalance.Of(run.State))
        {
            if (rate > PlannerConstants.LeftoverTolerance)
            {
                surplus[item] = rate;
            }
        }

        var bad = surplus.Keys
            .Where(k => PlannerConstants.Unsinkable.Contains(k) || context.Tier.Data.IsFluid(k))
            .Select(context.Tier.Data.ItemName)
            .ToList();
        if (bad.Count > 0)
        {
            throw new PlannerException("Left over but cannot be sunk: " + string.Join(", ", bad));
        }

        var disposed = new OrderedDictionary<ItemId, (double Rate, Recipe Recipe)>();
        foreach ((ItemId fluid, double rate) in disposal)
        {
            disposed[fluid] = (rate, disposalRecipes[fluid]);
        }

        return new ProductionPlan(machines.AsReadOnly(), extractors.AsReadOnly(), excessCopy, surplus, disposed);
    }

    /// <summary>Python <c>d[k] += x</c> on a <c>defaultdict(float)</c>: a missing key is inserted at the end.</summary>
    private static void AddTo<TKey>(OrderedDictionary<TKey, double> map, TKey key, double value)
        where TKey : notnull =>
        map[key] = map.GetValueOrDefault(key, 0.0) + value;

    /// <summary>One solve: the state plus the Python <c>_converge</c> and <c>_trim</c> steps.</summary>
    private sealed class Run
    {
        private readonly SolveContext _context;
        private readonly IReadOnlyList<(RecipeId Id, Recipe Recipe, int Machines)> _existing;

        public Run(SolveContext context)
        {
            _context = context;
            _existing = context.Existing.Machines;
        }

        public ProductionState State { get; } = new();

        /// <summary>Python <c>_converge</c>: cover every shortfall until there is none.</summary>
        public void Converge()
        {
            for (int iteration = 0; iteration < PlannerConstants.ConvergeIterations; iteration++)
            {
                var shortfalls = new List<(ItemId Item, double Quantity)>();
                foreach ((ItemId item, double balance) in ProductionBalance.Of(State))
                {
                    if (balance < -PlannerConstants.ShortfallTolerance)
                    {
                        shortfalls.Add((item, -balance));
                    }
                }

                if (shortfalls.Count == 0)
                {
                    return;
                }

                foreach ((ItemId item, double quantity) in shortfalls)
                {
                    Cover(item, quantity);
                }
            }

            throw new PlannerException("Did not converge");
        }

        /// <summary>
        /// Python <c>_trim</c>: remove machines and extraction made unnecessary by byproducts that arrived after they
        /// were sized, then settle again.
        /// </summary>
        /// <param name="disposedFluids">The fluids disposed of so far (Python <c>self.disposal</c> keys).</param>
        public void Trim(IEnumerable<ItemId> disposedFluids)
        {
            var disposalProducts = new HashSet<ItemId>();
            foreach (ItemId fluid in disposedFluids)
            {
                foreach (ItemAmount product in _context.DisposalFinder.Find(fluid).Products)
                {
                    disposalProducts.Add(product.Item);
                }
            }

            for (int iteration = 0; iteration < PlannerConstants.TrimIterations; iteration++)
            {
                bool cut = false;
                foreach ((ItemId item, double q) in ProductionBalance.Of(State))
                {
                    if (q <= PlannerConstants.TrimSurplusTolerance || disposalProducts.Contains(item))
                    {
                        continue;
                    }

                    cut |= _context.Extractors.IsExtractable(item) ? CutExtraction(item, q) : CutMachines(item, q);
                }

                if (!cut)
                {
                    return;
                }

                Converge();
            }
        }

        private void Cover(ItemId item, double quantity)
        {
            double q = quantity;
            if (_context.Extractors.IsExtractable(item))
            {
                AddTo(State.Extract, item, q);
                return;
            }

            // Spare capacity on machines built in earlier tiers comes first.
            foreach ((RecipeId id, Recipe existing, int have) in _existing)
            {
                if (existing.MainProduct.Item != item || q <= PlannerConstants.CapacityTolerance)
                {
                    continue;
                }

                double head = have - State.Runs.GetValueOrDefault(id, 0.0);
                if (head <= PlannerConstants.CapacityTolerance)
                {
                    continue;
                }

                double rate = existing.MainProduct.Amount * 60 / existing.TimeSeconds;
                double use = PythonNumerics.Min(head, q / rate);
                State.Recipes[id] = existing;
                AddTo(State.Runs, id, use);
                q -= use * rate;
            }

            if (q <= PlannerConstants.CapacityTolerance)
            {
                return;
            }

            Recipe recipe = _context.Choice.For(item)
                ?? throw new PlannerException($"No automatable source for {_context.Tier.Data.ItemName(item)}");
            State.Recipes[recipe.Id] = recipe;
            ItemAmount product = recipe.Products.First(p => p.Item == item);
            AddTo(State.Runs, recipe.Id, q / (product.Amount * 60 / recipe.TimeSeconds));
        }

        private bool CutExtraction(ItemId item, double q)
        {
            // Over-extracted raw resource.
            double d = PythonNumerics.Min(State.Extract.GetValueOrDefault(item, 0.0), q);
            if (d > PlannerConstants.TrimCutTolerance)
            {
                State.Extract[item] -= d;
                return true;
            }

            return false;
        }

        private bool CutMachines(ItemId item, double q)
        {
            Recipe? recipe = _context.Choice.For(item);
            if (recipe is null || !State.Runs.TryGetValue(recipe.Id, out double runs) || recipe.MainProduct.Item != item)
            {
                return false;
            }

            double rate = recipe.MainProduct.Amount * 60 / recipe.TimeSeconds;
            double d = PythonNumerics.Min(runs, q / rate);
            if (d > PlannerConstants.TrimCutTolerance)
            {
                State.Runs[recipe.Id] = runs - d;
                return true;
            }

            return false;
        }
    }
}
