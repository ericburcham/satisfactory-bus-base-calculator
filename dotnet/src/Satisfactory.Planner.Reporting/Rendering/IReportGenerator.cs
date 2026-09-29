using Satisfactory.Planner.Core.Tiers;

namespace Satisfactory.Planner.Reporting.Rendering;

/// <summary>Writes every report file of a planning run: per tier the build orders and graph, then the summary.</summary>
public interface IReportGenerator
{
    /// <summary>Writes the reports of every tier and the summary into <paramref name="outputFolder"/>.</summary>
    /// <param name="results">The planned tiers, in tier order.</param>
    /// <param name="outputFolder">The folder the reports are written to (it must exist).</param>
    /// <returns>The summary text (ending with the line terminator).</returns>
    string Generate(IReadOnlyList<TierResult> results, string outputFolder);
}
