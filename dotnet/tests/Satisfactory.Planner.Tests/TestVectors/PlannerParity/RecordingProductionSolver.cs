using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Solving;

namespace Satisfactory.Planner.Tests.TestVectors.PlannerParity;

/// <summary>
/// Decorator that records every solve (one per optimisation round) in the canonical form of
/// <see cref="PythonTierDump.Rounds"/>.
/// </summary>
internal sealed class RecordingProductionSolver(IProductionSolver inner) : IProductionSolver
{
    public List<(int Tier, string Outcome, IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)> Rounds { get; } = [];

    public ProductionPlan Solve(IReadOnlyDictionary<ItemId, double> excess, SolveContext context)
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["excess"] = excess.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["machines"] = [],
            ["extractors"] = [],
            ["sink"] = [],
            ["disposal"] = [],
        };
        ProductionPlan plan;
        try
        {
            plan = inner.Solve(excess, context);
        }
        catch (UndisposableFluidException e)
        {
            Rounds.Add((context.Tier.Tier, $"undisposable:{e.Fluid}", fields));
            throw;
        }

        fields["machines"] = plan.Machines.Select(m => $"{m.Recipe.Id}={PythonNumerics.Repr(m.Count)}").ToList();
        fields["extractors"] = plan.Extractors.Select(x => $"{x.Resource}={PythonNumerics.Repr(x.RatePerMinute)}").ToList();
        fields["sink"] = plan.Sink.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList();
        fields["disposal"] = plan.Disposal
            .Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value.Rate)}|{e.Value.Recipe.Id}")
            .ToList();
        Rounds.Add((context.Tier.Tier, "ok", fields));
        return plan;
    }
}
