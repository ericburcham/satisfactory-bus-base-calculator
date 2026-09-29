using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>Writes one report file for a planned tier.</summary>
public interface ITierReportWriter
{
    /// <summary>Writes the report of <paramref name="result"/> into <see cref="TierReportContext.OutputFolder"/>.</summary>
    /// <param name="result">The planned tier.</param>
    /// <param name="context">The tier's graph, build order, totals and the output folder.</param>
    void Write(TierResult result, TierReportContext context);
}
