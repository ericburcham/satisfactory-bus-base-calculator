using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Model;

/// <summary>
/// A data file recipe made in a machine (<c>inMachine</c> is true and <c>producedIn</c> is not empty).
/// </summary>
/// <param name="Id">The recipe's <c>className</c>.</param>
/// <param name="Name">The display name (<c>name</c>).</param>
/// <param name="IsAlternate">The <c>alternate</c> flag.</param>
/// <param name="TimeSeconds">The cycle time in seconds (<c>time</c>).</param>
/// <param name="BuildingId">The machine that runs it (<c>producedIn[0]</c>).</param>
/// <param name="Ingredients">The ingredients per cycle, in data file order.</param>
/// <param name="Products">The products per cycle, in data file order.</param>
/// <param name="IsVariablePower">The <c>isVariablePower</c> flag (<see langword="false"/> when absent).</param>
/// <param name="MinPowerMw">The <c>minPower</c> in MW (0 when absent).</param>
/// <param name="MaxPowerMw">The <c>maxPower</c> in MW (0 when absent).</param>
public sealed record ManufacturingRecipe(
    RecipeId Id,
    string Name,
    bool IsAlternate,
    double TimeSeconds,
    BuildingId BuildingId,
    IReadOnlyList<ItemAmount> Ingredients,
    IReadOnlyList<ItemAmount> Products,
    bool IsVariablePower,
    double MinPowerMw,
    double MaxPowerMw)
    : Recipe(Id, Name, IsAlternate, TimeSeconds, BuildingId, Ingredients, Products);
