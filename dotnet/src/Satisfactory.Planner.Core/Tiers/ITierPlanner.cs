using Satisfactory.Planner.Core.Configuration;

namespace Satisfactory.Planner.Core.Tiers;

/// <summary>Plans every progression tier in order (the Python <c>run_tiers</c>).</summary>
public interface ITierPlanner
{
    /// <summary>Plans tiers <see cref="PlannerConstants.FirstTier"/> to <see cref="PlannerConstants.LastTier"/>.</summary>
    /// <param name="options">The planner options; only <see cref="PlannerOptions.IncludeIngots"/> affects planning here.</param>
    /// <returns>One result per tier, in tier order.</returns>
    IReadOnlyList<TierResult> PlanAllTiers(PlannerOptions options);
}
