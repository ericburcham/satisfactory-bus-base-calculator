using System.Globalization;
using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>Display names of tiers (Python <c>TIER_NAMES.get(t, f"Tier {t}")</c>).</summary>
public static class TierNames
{
    /// <summary>The display name of a tier.</summary>
    /// <param name="tier">The tier.</param>
    /// <returns>The name from <see cref="PlannerConstants.TierNames"/>, or <c>Tier {tier}</c>.</returns>
    public static string For(int tier) =>
        PlannerConstants.TierNames.TryGetValue(tier, out string? name)
            ? name
            : string.Create(CultureInfo.InvariantCulture, $"Tier {tier}");
}
