using System.Globalization;
using Satisfactory.Planner.Core.Numerics;
using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Tests.TestVectors.PlannerParity;

/// <summary>
/// The canonical entry strings of a .NET <see cref="TierResult"/>, field by field in the same form as
/// <see cref="PythonTierDump"/> (floats as <see cref="PythonNumerics.Repr"/>, which is bit-exact).
/// </summary>
internal static class TierResultCanonicalForm
{
    /// <summary>The canonical fields of a tier result.</summary>
    /// <param name="result">The result.</param>
    /// <returns>Field name to its entries, in dictionary order.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Of(TierResult result) =>
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["new_items"] = result.NewItems.Select(i => i.Value).ToList(),
            ["new"] = result.NewMachines.Select(e => $"{e.Key}={Int(e.Value)}").ToList(),
            ["runs"] = result.Runs.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["recs"] = result.Recipes.Keys.Select(k => k.Value).ToList(),
            ["existing"] = result.ExistingBefore.Select(e => $"{e.Key}={Int(e.Value)}").ToList(),
            ["excess"] = result.Excess.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["excess_recipe"] = result.ExcessRecipe.Select(e => $"{e.Key}={e.Value.Id}").ToList(),
            ["reused"] = result.Reused.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["raw"] = result.RawInput.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["sink"] = result.Sink.Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value)}").ToList(),
            ["disposal"] = result.Disposal
                .Select(e => $"{e.Key}={PythonNumerics.Repr(e.Value.Rate)}|{e.Value.Recipe.Id}")
                .ToList(),
        };

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
}
