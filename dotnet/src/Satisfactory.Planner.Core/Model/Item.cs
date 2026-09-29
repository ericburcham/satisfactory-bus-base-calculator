using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>An entry of the data file's <c>items</c> object.</summary>
/// <param name="Id">The item's class name (its key in <c>items</c>).</param>
/// <param name="Name">The display name (<c>name</c>).</param>
/// <param name="IsFluid">The <c>liquid</c> flag (Python default <see langword="false"/> when absent).</param>
/// <param name="StackSize">The <c>stackSize</c> (0 when absent; the planner does not use it).</param>
/// <param name="SinkPoints">The <c>sinkPoints</c> (0 when absent; the planner does not use it).</param>
/// <param name="EnergyValue">The <c>energyValue</c> in MJ (0 when absent); the fuel rods' value sizes the generator recipes.</param>
public sealed record Item(ItemId Id, string Name, bool IsFluid, int StackSize, int SinkPoints, double EnergyValue);
