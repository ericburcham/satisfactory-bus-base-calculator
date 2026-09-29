namespace Satisfactory.Planner.Core.Configuration;

/// <summary>The planner's run options (the Python command line arguments).</summary>
/// <param name="DataPath">The game data file (<c>--data</c>); downloaded there when missing.</param>
/// <param name="OutputFolder">The folder the reports are written to (<c>--out</c>).</param>
/// <param name="MinerTier">The miner for solid resources (<c>--miner</c>).</param>
/// <param name="NodePurity">The node purity for solid resources (<c>--purity</c>).</param>
/// <param name="IncludeIngots">Whether ingots also become excess targets (<c>--include-ingots</c>).</param>
public sealed record PlannerOptions(
    string DataPath,
    string OutputFolder,
    MinerTier MinerTier,
    NodePurity NodePurity,
    bool IncludeIngots);
