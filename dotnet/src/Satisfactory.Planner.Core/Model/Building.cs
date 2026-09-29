using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>An entry of the data file's <c>buildings</c> object.</summary>
/// <param name="Id">The building's class name (its key in <c>buildings</c>).</param>
/// <param name="Name">The display name (<c>name</c>).</param>
/// <param name="PowerConsumptionMw">
/// <c>metadata.powerConsumption</c> in MW, or <see langword="null"/> when the data file has no such field.
/// </param>
/// <param name="PowerConsumptionIsInteger">
/// <see langword="true"/> when the JSON token of <c>powerConsumption</c> is an integer literal (no <c>.</c>, <c>e</c> or
/// <c>E</c>), so that Python computes the power of whole machines as an <c>int</c> and prints it without a decimal point.
/// </param>
public sealed record Building(BuildingId Id, string Name, double? PowerConsumptionMw, bool PowerConsumptionIsInteger);
