using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Power;

/// <summary>
/// Python <c>_power</c>: a generator produces <see cref="GeneratorRecipe.PowerOutputMw"/> per machine (negative result);
/// a variable-power recipe with a positive maximum uses the average of its minimum and maximum; any other recipe uses
/// its building's <c>powerConsumption</c>.
/// </summary>
public sealed class PowerCalculator : IPowerCalculator
{
    private readonly GameData _data;

    /// <summary>Creates the calculator.</summary>
    /// <param name="data">The game data holding the buildings.</param>
    public PowerCalculator(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    /// <inheritdoc />
    /// <exception cref="GameDataFormatException">The recipe's building is unknown or has no <c>powerConsumption</c>.</exception>
    public double PowerMw(Recipe recipe, double machines)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (recipe is GeneratorRecipe generator)
        {
            return -generator.PowerOutputMw * machines;
        }

        Building building = BuildingOf(recipe);
        if (IsVariablePower(recipe, out ManufacturingRecipe? variable))
        {
            double average = (variable.MinPowerMw + variable.MaxPowerMw) / 2;
            return average * machines;
        }

        return PowerConsumptionOf(building, recipe) * machines;
    }

    /// <inheritdoc />
    /// <exception cref="GameDataFormatException">The recipe's building is unknown or has no <c>powerConsumption</c>.</exception>
    public bool IsIntegerValued(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (recipe is GeneratorRecipe)
        {
            return true;
        }

        Building building = BuildingOf(recipe);
        if (IsVariablePower(recipe, out _))
        {
            return false;
        }

        _ = PowerConsumptionOf(building, recipe);
        return building.PowerConsumptionIsInteger;
    }

    private static bool IsVariablePower(Recipe recipe, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ManufacturingRecipe? variable)
    {
        if (recipe is ManufacturingRecipe { IsVariablePower: true, MaxPowerMw: > 0 } manufacturing)
        {
            variable = manufacturing;
            return true;
        }

        variable = null;
        return false;
    }

    private static double PowerConsumptionOf(Building building, Recipe recipe) =>
        building.PowerConsumptionMw
        ?? throw new GameDataFormatException(
            $"Building {building.Id} (used by recipe {recipe.Id}) has no metadata.powerConsumption.");

    private Building BuildingOf(Recipe recipe) =>
        _data.TryGetBuilding(recipe.BuildingId, out Building? building)
            ? building
            : throw new GameDataFormatException($"Unknown building {recipe.BuildingId} (used by recipe {recipe.Id}).");
}
