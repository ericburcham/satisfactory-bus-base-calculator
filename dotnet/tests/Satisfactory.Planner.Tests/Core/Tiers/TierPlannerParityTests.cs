using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Tests.TestSupport;
using Satisfactory.Planner.Tests.TestVectors.PlannerParity;

namespace Satisfactory.Planner.Tests.Core.Tiers;

/// <summary>
/// Runs the real .NET planning pipeline on <c>data/satisfactory_data.json</c> and compares every tier result, and every
/// optimisation round, with the Python reference's dump (<c>dump_python_tiers.py</c>): the same keys in the same order
/// and bit-identical floats (compared as Python <c>repr</c> strings, which round-trip exactly).
/// </summary>
[TestFixture]
[Category("Parity")]
public sealed class TierPlannerParityTests
{
    private const string DefaultDump = "python_tier_results.json";
    private const string Mk2PureIngotsDump = "python_tier_results_mk2_pure_ingots.json";

    [TestCase(DefaultDump)]
    [TestCase(Mk2PureIngotsDump)]
    public void PlanAllTiers_MatchesPythonRunTiersExactly(string dumpFile)
    {
        var expected = PythonTierDump.Load(dumpFile);

        (IReadOnlyList<TierResult> actual, _) = Run(expected.Options);

        Assert.That(actual.Select(r => r.Tier), Is.EqualTo(expected.Tiers.Select(t => t.Tier)));
        var mismatches = new List<string>();
        for (int index = 0; index < expected.Tiers.Count; index++)
        {
            (int tier, IReadOnlyDictionary<string, IReadOnlyList<string>> python) = expected.Tiers[index];
            Compare($"tier {tier}", PythonTierDump.FieldNames, python, TierResultCanonicalForm.Of(actual[index]), mismatches);
        }

        Assert.That(mismatches, Is.Empty, () => string.Join(Environment.NewLine, mismatches));
    }

    [TestCase(DefaultDump)]
    [TestCase(Mk2PureIngotsDump)]
    public void PlanAllTiers_EveryOptimisationRoundMatchesPython(string dumpFile)
    {
        var expected = PythonTierDump.Load(dumpFile);

        (_, RecordingProductionSolver recorder) = Run(expected.Options);

        Assert.That(
            recorder.Rounds.Select(r => $"{r.Tier}:{r.Outcome}"),
            Is.EqualTo(expected.Rounds.Select(r => $"{r.Tier}:{r.Outcome}")));
        var mismatches = new List<string>();
        for (int index = 0; index < expected.Rounds.Count; index++)
        {
            Compare(
                $"round {index} (tier {expected.Rounds[index].Tier})",
                PythonTierDump.RoundFieldNames,
                expected.Rounds[index].Fields,
                recorder.Rounds[index].Fields,
                mismatches);
        }

        Assert.That(mismatches, Is.Empty, () => string.Join(Environment.NewLine, mismatches));
    }

    [Test]
    public void Dumps_ExerciseSinkAndDisposalRounds()
    {
        var dump = PythonTierDump.Load(DefaultDump);

        Assert.Multiple(() =>
        {
            Assert.That(dump.Tiers, Has.Count.EqualTo(PlannerConstants.LastTier - PlannerConstants.FirstTier + 1));
            Assert.That(dump.Rounds.Count(r => r.Fields["sink"].Count > 0), Is.Positive);
            Assert.That(dump.Rounds.Count(r => r.Fields["disposal"].Count > 0), Is.Positive);
            Assert.That(dump.Tiers.Count(t => t.Fields["reused"].Count > 0), Is.Positive);
        });
    }

    private static (IReadOnlyList<TierResult> Results, RecordingProductionSolver Recorder) Run(PlannerOptions options)
    {
        GameData data = CoreRealGameData.Data;
        var contexts = new TierContextFactory(data, new UnlockTierResolver(data), options.MinerTier, options.NodePurity);
        var costs = new ResourceCostCalculator(new DefaultResourceWeights());
        var selector = new CheapestRecipeSelector();
        var recorder = new RecordingProductionSolver(new ProductionSolver(new PowerCalculator(data)));
        var optimizer = new ByproductCreditOptimizer(
            costs, selector, new OneMachineExcessPolicy(), recorder, new FluidDisposalFinderFactory());
        var planner = new TierPlanner(contexts, costs, selector, optimizer);
        return (planner.PlanAllTiers(options), recorder);
    }

    private static void Compare(
        string label,
        IReadOnlyList<string> fieldNames,
        IReadOnlyDictionary<string, IReadOnlyList<string>> python,
        IReadOnlyDictionary<string, IReadOnlyList<string>> dotnet,
        List<string> mismatches)
    {
        foreach (string field in fieldNames)
        {
            string? difference = FirstDifference(python[field], dotnet[field]);
            if (difference is not null)
            {
                mismatches.Add($"{label} {field}: {difference}");
            }
        }
    }

    private static string? FirstDifference(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        for (int index = 0; index < Math.Max(expected.Count, actual.Count); index++)
        {
            string? e = index < expected.Count ? expected[index] : null;
            string? a = index < actual.Count ? actual[index] : null;
            if (!string.Equals(e, a, StringComparison.Ordinal))
            {
                return $"entry {index}: python '{e ?? "<none>"}' vs .NET '{a ?? "<none>"}' "
                    + $"(python {expected.Count} entries, .NET {actual.Count})";
            }
        }

        return null;
    }
}
