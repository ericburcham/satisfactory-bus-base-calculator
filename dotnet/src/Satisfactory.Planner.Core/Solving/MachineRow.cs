using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>A machine group of a plan (Python row with <c>kind="machine"</c>).</summary>
/// <param name="Recipe">The recipe the machines run.</param>
/// <param name="Count">The exact (fractional) number of machines at 100% clock; may be 0 after trimming.</param>
/// <param name="PowerMw">The power of <paramref name="Count"/> machines (negative when generated).</param>
public sealed record MachineRow(Recipe Recipe, double Count, double PowerMw);
