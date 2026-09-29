using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>Writes the Graphviz <c>.dot</c> file of a tier.</summary>
public interface ITierDotWriter
{
    /// <summary>Writes the tier's <c>.dot</c> file into <see cref="TierReportContext.OutputFolder"/>.</summary>
    /// <param name="result">The planned tier.</param>
    /// <param name="context">The tier's graph, build order, totals and the output folder.</param>
    /// <returns>The path of the written <c>.dot</c> file.</returns>
    string Write(TierResult result, TierReportContext context);
}
