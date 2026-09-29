using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Power;

[TestFixture]
public sealed class PowerCalculatorTests
{
    private static GameData Sample() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_DarkMatter_C", "Dark Matter")
        .WithFluid("Desc_Water_C", "Water")
        .WithItem("Desc_NuclearFuelRod_C", "Uranium Fuel Rod", energyValue: 750000)
        .WithItem("Desc_PlutoniumFuelRod_C", "Plutonium Fuel Rod", energyValue: 1500000)
        .WithItem("Desc_NuclearWaste_C", "Uranium Waste")
        .WithItem("Desc_PlutoniumWaste_C", "Plutonium Waste")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithBuilding("Desc_FoundryMk1_C", "Foundry", 16.5, isInteger: false)
        .WithBuilding("Desc_HadronCollider_C", "Particle Accelerator", 1500)
        .WithBuilding("Desc_Beam_C", "Beam", null)
        .WithBuilding("Desc_GeneratorNuclear_C", "Nuclear Power Plant", 0)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipe("Recipe_Foundry_C", "Foundry Ingot", "Desc_FoundryMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipe("Recipe_Beam_C", "Beam Ingot", "Desc_Beam_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithVariablePowerRecipe(
            "Recipe_DarkMatter_C", "Dark Matter", "Desc_HadronCollider_C", 2, [("Desc_IronIngot_C", 1)], [("Desc_DarkMatter_C", 1)], 500, 1500)
        .WithVariablePowerRecipe(
            "Recipe_ZeroMax_C", "Zero Max", "Desc_HadronCollider_C", 2, [("Desc_IronIngot_C", 1)], [("Desc_DarkMatter_C", 1)], 0, 0)
        .WithNuclearGeneratorRecipes()
        .Build();

    private static Recipe RecipeOf(GameData data, string id) => data.Recipes.Single(r => r.Id == new RecipeId(id));

    [Test]
    public void FixedPower_IsBuildingPowerTimesMachines()
    {
        GameData data = Sample();
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_IngotIron_C"), 1), Is.EqualTo(4.0));
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_IngotIron_C"), 2.5), Is.EqualTo(10.0));
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_Foundry_C"), 3), Is.EqualTo(16.5 * 3));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_IngotIron_C")), Is.True);
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_Foundry_C")), Is.False);
        });
    }

    [Test]
    public void VariablePower_IsTheAverageTimesMachines()
    {
        GameData data = Sample();
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_DarkMatter_C"), 1), Is.EqualTo(1000.0));
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_DarkMatter_C"), 3), Is.EqualTo(3000.0));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_DarkMatter_C")), Is.False);
        });
    }

    [Test]
    public void VariablePowerWithZeroMaximum_UsesTheBuildingPower()
    {
        // Python: r.get("isVariablePower") and r["maxPower"] > 0 -- otherwise the building's powerConsumption.
        GameData data = Sample();
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_ZeroMax_C"), 2), Is.EqualTo(3000.0));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_ZeroMax_C")), Is.True);
        });
    }

    [Test]
    public void Generator_IsNegativeOutputTimesMachines()
    {
        GameData data = Sample();
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(power.PowerMw(RecipeOf(data, "Gen_Nuclear_Uranium"), 12), Is.EqualTo(-30000.0));
            Assert.That(power.PowerMw(RecipeOf(data, "Gen_Nuclear_Plutonium"), 1), Is.EqualTo(-2500.0));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Gen_Nuclear_Uranium")), Is.True);
        });
    }

    [Test]
    public void MissingPowerConsumption_Throws()
    {
        GameData data = Sample();
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => power.PowerMw(RecipeOf(data, "Recipe_Beam_C"), 1),
                Throws.TypeOf<GameDataFormatException>().With.Message.Contains("Desc_Beam_C"));
            Assert.That(
                () => power.IsIntegerValued(RecipeOf(data, "Recipe_Beam_C")),
                Throws.TypeOf<GameDataFormatException>());
        });
    }

    [Test]
    public void UnknownBuilding_Throws()
    {
        GameData data = Sample();
        var power = new PowerCalculator(new GameData(data.Items, [], [], [], data.Recipes, []));

        Assert.That(
            () => power.PowerMw(RecipeOf(data, "Recipe_IngotIron_C"), 1),
            Throws.TypeOf<GameDataFormatException>().With.Message.Contains("Desc_SmelterMk1_C"));
    }

    [Test]
    public void RealData_SmelterAndGenerators()
    {
        GameData data = CoreRealGameData.Data;
        var power = new PowerCalculator(data);

        Assert.Multiple(() =>
        {
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_IngotIron_C"), 1), Is.EqualTo(4.0));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_IngotIron_C")), Is.True);
            Assert.That(power.PowerMw(RecipeOf(data, "Recipe_DarkMatter_C"), 1), Is.EqualTo(1000.0));
            Assert.That(power.IsIntegerValued(RecipeOf(data, "Recipe_DarkMatter_C")), Is.False);
            Assert.That(power.PowerMw(RecipeOf(data, "Gen_Nuclear_Uranium"), 12), Is.EqualTo(-30000.0));
        });
    }
}
