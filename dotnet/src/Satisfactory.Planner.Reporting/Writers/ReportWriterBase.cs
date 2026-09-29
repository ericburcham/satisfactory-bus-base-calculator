using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Template Method for a tier report file: <see cref="Write"/> renders the content with <see cref="Render"/> and writes
/// it to <see cref="FileName"/> inside the output folder.
/// </summary>
public abstract class ReportWriterBase : ITierReportWriter
{
    private readonly IFileSystem _fileSystem;

    /// <summary>Initializes the writer.</summary>
    /// <param name="fileSystem">The file system the report is written to.</param>
    protected ReportWriterBase(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Writes <see cref="Render"/>'s content to <c>Path.Combine(context.OutputFolder, FileName(result))</c>, replacing
    /// any existing file.
    /// </summary>
    /// <param name="result">The planned tier.</param>
    /// <param name="context">The tier's graph, build order, totals and the output folder.</param>
    public void Write(TierResult result, TierReportContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        string path = Path.Combine(context.OutputFolder, FileName(result));
        _fileSystem.WriteAllText(path, Render(result, context));
    }

    /// <summary>The report's file name (without folder), such as <c>tier_3_order.txt</c>.</summary>
    /// <param name="result">The planned tier.</param>
    /// <returns>The file name.</returns>
    protected abstract string FileName(TierResult result);

    /// <summary>Renders the complete file content.</summary>
    /// <param name="result">The planned tier.</param>
    /// <param name="context">The tier's graph, build order, totals and the output folder.</param>
    /// <returns>The file content, verbatim (line terminators included).</returns>
    protected abstract string Render(TierResult result, TierReportContext context);
}
