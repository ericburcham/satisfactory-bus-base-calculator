namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>The totals of one tier's new machines (the values Python <c>write_tier</c> returns).</summary>
/// <param name="NewMachines">The number of new machines (Python <c>total_new</c>).</param>
/// <param name="PowerUsedMw">The power the new machines use at 100% clock, in MW (Python <c>mw</c>).</param>
/// <param name="PowerGeneratedMw">The power the new generators produce, in MW, as a positive number (Python <c>gen</c>).</param>
public sealed record TierTotals(int NewMachines, double PowerUsedMw, double PowerGeneratedMw);
