using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>An ingredient, product or cost entry: an item and an amount (per recipe cycle, or per schematic).</summary>
/// <param name="Item">The item.</param>
/// <param name="Amount">The amount (fluids are already in m3 in the data file).</param>
public sealed record ItemAmount(ItemId Item, double Amount);
