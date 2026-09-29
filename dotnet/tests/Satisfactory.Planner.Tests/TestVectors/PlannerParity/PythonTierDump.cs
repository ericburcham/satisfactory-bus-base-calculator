using System.Text.Json;
using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Tests.TestVectors.PlannerParity;

/// <summary>
/// Loads a dump written by <c>dump_python_tiers.py</c> (the Python reference's <c>run_tiers</c> results) from the test
/// output directory. Every tier field becomes a list of canonical entry strings in Python dict order, the same strings
/// <see cref="TierResultCanonicalForm"/> produces for a .NET <c>TierResult</c>: <c>key</c> for lists of keys,
/// <c>key=value</c> for maps and <c>key=rate|recipe</c> for the disposal map; floats are Python <c>repr</c>s.
/// </summary>
internal sealed class PythonTierDump
{
    /// <summary>The tier fields, in dump order.</summary>
    public static readonly IReadOnlyList<string> FieldNames =
    [
        "new_items", "new", "runs", "recs", "existing", "excess", "excess_recipe", "reused", "raw", "sink", "disposal",
    ];

    /// <summary>The fields of each recorded optimisation round, in dump order.</summary>
    public static readonly IReadOnlyList<string> RoundFieldNames = ["excess", "machines", "extractors", "sink", "disposal"];

    private PythonTierDump(
        string pythonVersion,
        PlannerOptions options,
        IReadOnlyList<(int Tier, IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)> tiers,
        IReadOnlyList<(int Tier, string Outcome, IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)> rounds)
    {
        PythonVersion = pythonVersion;
        Options = options;
        Tiers = tiers;
        Rounds = rounds;
    }

    /// <summary>
    /// Every <c>plan()</c> call of the run (one per optimisation round): tier, outcome (<c>ok</c> or
    /// <c>undisposable:&lt;fluid&gt;</c>) and the canonical <see cref="RoundFieldNames"/> fields.
    /// </summary>
    public IReadOnlyList<(int Tier, string Outcome, IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)> Rounds { get; }

    /// <summary>The version of the Python that wrote the dump.</summary>
    public string PythonVersion { get; }

    /// <summary>The planner options the dump was made with (data path and output folder are placeholders).</summary>
    public PlannerOptions Options { get; }

    /// <summary>Per tier: the tier number and its canonical fields.</summary>
    public IReadOnlyList<(int Tier, IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)> Tiers { get; }

    /// <summary>Loads a dump file from <c>TestVectors/PlannerParity/</c> in the test output directory.</summary>
    /// <param name="fileName">The dump's file name.</param>
    /// <returns>The dump.</returns>
    public static PythonTierDump Load(string fileName)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestVectors", "PlannerParity", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        JsonElement options = root.GetProperty("options");
        var plannerOptions = new PlannerOptions(
            PlannerConstants.DefaultDataPath,
            PlannerConstants.DefaultOutputFolder,
            Enum.Parse<MinerTier>(options.GetProperty("miner").GetString()!, ignoreCase: true),
            Enum.Parse<NodePurity>(options.GetProperty("purity").GetString()!, ignoreCase: true),
            options.GetProperty("include_ingots").GetBoolean());
        var tiers = new List<(int, IReadOnlyDictionary<string, IReadOnlyList<string>>)>();
        foreach (JsonElement tier in root.GetProperty("tiers").EnumerateArray())
        {
            tiers.Add((tier.GetProperty("tier").GetInt32(), Fields(tier, FieldNames)));
        }

        var rounds = new List<(int, string, IReadOnlyDictionary<string, IReadOnlyList<string>>)>();
        foreach (JsonElement round in root.GetProperty("rounds").EnumerateArray())
        {
            rounds.Add((round.GetProperty("tier").GetInt32(), round.GetProperty("outcome").GetString()!, Fields(round, RoundFieldNames)));
        }

        return new PythonTierDump(root.GetProperty("python").GetString()!, plannerOptions, tiers, rounds);
    }

    private static Dictionary<string, IReadOnlyList<string>> Fields(JsonElement element, IReadOnlyList<string> names)
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            fields[name] = element.GetProperty(name).EnumerateArray().Select(Canonical).ToList();
        }

        return fields;
    }

    private static string Canonical(JsonElement entry)
    {
        if (entry.ValueKind == JsonValueKind.String)
        {
            return entry.GetString()!;
        }

        var parts = entry.EnumerateArray().Select(Scalar).ToList();
        return parts.Count switch
        {
            2 => $"{parts[0]}={parts[1]}",
            3 => $"{parts[0]}={parts[1]}|{parts[2]}",
            _ => throw new FormatException($"Unexpected dump entry {entry}."),
        };
    }

    private static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.Number => value.GetRawText(),
        _ => throw new FormatException($"Unexpected dump value {value}."),
    };
}
