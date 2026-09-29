using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>Writes the summary table of all tiers.</summary>
public interface ISummaryWriter
{
    /// <summary>Writes <c>summary.txt</c> into <paramref name="outputFolder"/>.</summary>
    /// <param name="tiers">Every planned tier with its totals, in tier order.</param>
    /// <param name="outputFolder">The folder the summary is written to.</param>
    /// <returns>The summary text as written (ending with the line terminator).</returns>
    string Write(IReadOnlyList<(TierResult Result, TierTotals Totals)> tiers, string outputFolder);
}
