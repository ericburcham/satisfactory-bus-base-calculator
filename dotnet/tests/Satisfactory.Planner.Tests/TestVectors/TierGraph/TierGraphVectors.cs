using System.Text.Json;

namespace Satisfactory.Planner.Tests.TestVectors.TierGraph;

/// <summary>
/// Loads <c>TierGraph/tier_graph_vectors.json</c> (written by <c>generate_tier_graph_vectors.py</c> from the Python
/// <c>run_tiers</c> with the golden-output defaults) from the test output directory: per tier, what the graph needs of
/// the Python tier result, plus the Python build order.
/// </summary>
internal sealed class TierGraphVectors
{
    private static readonly Lazy<TierGraphVectors> LazyInstance = new(Load);

    private TierGraphVectors(string pythonVersion, IReadOnlyList<TierGraphVector> tiers)
    {
        PythonVersion = pythonVersion;
        Tiers = tiers;
    }

    /// <summary>The vectors, loaded once per test run.</summary>
    public static TierGraphVectors Instance => LazyInstance.Value;

    /// <summary>The version of the Python that generated the file.</summary>
    public string PythonVersion { get; }

    /// <summary>One entry per tier, in tier order.</summary>
    public IReadOnlyList<TierGraphVector> Tiers { get; }

    private static TierGraphVectors Load()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestVectors", "TierGraph", "tier_graph_vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        var tiers = root.GetProperty("tiers").EnumerateArray()
            .Select(e => new TierGraphVector(
                e.GetProperty("tier").GetInt32(),
                Strings(e.GetProperty("newItems")),
                e.GetProperty("new").EnumerateArray().Select(p => (p[0].GetString()!, p[1].GetInt32())).ToList(),
                Strings(e.GetProperty("recipes")),
                e.GetProperty("sink").EnumerateArray().Select(p => (p[0].GetString()!, p[1].GetDouble())).ToList(),
                Strings(e.GetProperty("order"))))
            .ToList();
        return new TierGraphVectors(root.GetProperty("python").GetString()!, tiers);
    }

    private static List<string> Strings(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetString()!).ToList();
}

/// <summary>The Python planner's decisions for one tier, as far as the tier graph needs them.</summary>
/// <param name="Tier">The tier number.</param>
/// <param name="NewItems">Python <c>res["new_items"]</c>.</param>
/// <param name="NewMachines">Python <c>res["new"]</c>, in insertion order.</param>
/// <param name="Recipes">The keys of Python <c>res["recs"]</c>, in insertion order.</param>
/// <param name="Sink">Python <c>res["sink"]</c>, in insertion order.</param>
/// <param name="Order">The Python build order (<c>tier_order(p, res)[0]</c>).</param>
internal sealed record TierGraphVector(
    int Tier,
    IReadOnlyList<string> NewItems,
    IReadOnlyList<(string Recipe, int Machines)> NewMachines,
    IReadOnlyList<string> Recipes,
    IReadOnlyList<(string Item, double Rate)> Sink,
    IReadOnlyList<string> Order);
