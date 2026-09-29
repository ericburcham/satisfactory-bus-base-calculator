using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Power;

/// <summary>Power of machines running a recipe at 100% clock (Python <c>_power</c>).</summary>
public interface IPowerCalculator
{
    /// <summary>The power of <paramref name="machines"/> machines running <paramref name="recipe"/>.</summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="machines">The number of machines (may be fractional).</param>
    /// <returns>The power in MW; negative when generated.</returns>
    double PowerMw(Recipe recipe, double machines);

    /// <summary>
    /// Whether Python computes this recipe's power for a whole number of machines as an <c>int</c> (so <c>str()</c>
    /// prints no decimal point): generators, and non-variable-power recipes whose building's <c>powerConsumption</c> is
    /// an integer literal.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <returns><see langword="true"/> for integer-typed power.</returns>
    bool IsIntegerValued(Recipe recipe);
}
