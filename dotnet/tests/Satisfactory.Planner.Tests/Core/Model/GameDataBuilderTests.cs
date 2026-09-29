using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Model;

[TestFixture]
public sealed class GameDataBuilderTests
{
    private static GameDataBuilder Base() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
        .WithFluid("Desc_Water_C", "Water").WithResource("Desc_Water_C")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_NuclearFuelRod_C", "Uranium Fuel Rod", energyValue: 750000)
        .WithItem("Desc_PlutoniumFuelRod_C", "Plutonium Fuel Rod", energyValue: 1500000)
        .WithItem("Desc_NuclearWaste_C", "Uranium Waste")
        .WithItem("Desc_PlutoniumWaste_C", "Plutonium Waste")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithBuilding("Desc_HadronCollider_C", "Particle Accelerator", 1500)
        .WithBuilding("Desc_GeneratorNuclear_C", "Nuclear Power Plant", 0);

    [Test]
    public void Recipes_AreManufacturingThenGenerators_DefinitionsInInsertionOrder()
    {
        GameData data = Base()
            .WithGeneratorRecipe(
                "Gen_Custom", "Burn", "Desc_GeneratorNuclear_C", "Nuclear Power Plant", 10,
                [("Desc_NuclearFuelRod_C", 1)], [("Desc_NuclearWaste_C", 1)], 100)
            .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
            .WithBuildRecipe("Desc_SmelterMk1_C")
            .WithVariablePowerRecipe(
                "Recipe_Var_C", "Var", "Desc_HadronCollider_C", 2, [("Desc_IronIngot_C", 1)], [("Desc_OreIron_C", 1)], 100, 300, alternate: true)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(data.Recipes.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C", "Recipe_Var_C", "Gen_Custom")));
            Assert.That(
                data.RecipeDefinitions.Select(r => r.Id.Value),
                Is.EqualTo(ExpectedSequence.Of("Gen_Custom", "Recipe_IngotIron_C", "Recipe_SmelterMk1_C", "Recipe_Var_C")));
            Assert.That(data.RecipeDefinitions[2].Products, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_SmelterMk1_C"), 1))));
            var variable = (ManufacturingRecipe)data.Recipes[1];
            Assert.That(variable.IsVariablePower, Is.True);
            Assert.That(variable.IsAlternate, Is.True);
            Assert.That(variable.MinPowerMw, Is.EqualTo(100));
            Assert.That(variable.MaxPowerMw, Is.EqualTo(300));
            Assert.That(((GeneratorRecipe)data.Recipes[2]).PowerOutputMw, Is.EqualTo(100));
        });
    }

    [Test]
    public void NuclearGenerators_MatchTheJsonLoader()
    {
        GameData data = Base().WithNuclearGeneratorRecipes().Build();

        var uranium = (GeneratorRecipe)data.Recipes[0];
        var plutonium = (GeneratorRecipe)data.Recipes[1];
        Assert.Multiple(() =>
        {
            Assert.That(uranium.Id.Value, Is.EqualTo("Gen_Nuclear_Uranium"));
            Assert.That(uranium.Name, Is.EqualTo("Burn Uranium Fuel Rod"));
            Assert.That(uranium.TimeSeconds, Is.EqualTo(300));
            Assert.That(uranium.Ingredients[1], Is.EqualTo(new ItemAmount(new ItemId("Desc_Water_C"), 1200)));
            Assert.That(uranium.Products[0], Is.EqualTo(new ItemAmount(new ItemId("Desc_NuclearWaste_C"), 50)));
            Assert.That(uranium.BuildingName, Is.EqualTo("Nuclear Power Plant"));
            Assert.That(uranium.PowerOutputMw, Is.EqualTo(2500));
            Assert.That(plutonium.TimeSeconds, Is.EqualTo(600));
            Assert.That(plutonium.Products[0], Is.EqualTo(new ItemAmount(new ItemId("Desc_PlutoniumWaste_C"), 10)));
        });
    }

    [Test]
    public void Building_IntegerFlagAndMissingPower()
    {
        GameData data = new GameDataBuilder()
            .WithBuilding("Desc_A_C", "A", 16.5, isInteger: false)
            .WithBuilding("Desc_B_C", "B", null)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(data.Buildings[0], Is.EqualTo(new Building(new BuildingId("Desc_A_C"), "A", 16.5, false)));
            Assert.That(data.Buildings[1], Is.EqualTo(new Building(new BuildingId("Desc_B_C"), "B", null, false)));
        });
    }

    [Test]
    public void Build_RejectsUndeclaredReferences()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Base().WithRecipe("Recipe_X_C", "X", "Desc_SmelterMk1_C", 1, [("Desc_Nope_C", 1)], [("Desc_IronIngot_C", 1)]).Build(),
                Throws.InvalidOperationException.With.Message.Contains("Desc_Nope_C"));
            Assert.That(
                () => Base().WithRecipe("Recipe_X_C", "X", "Desc_Nope_C", 1, [], [("Desc_IronIngot_C", 1)]).Build(),
                Throws.InvalidOperationException.With.Message.Contains("Desc_Nope_C"));
            Assert.That(
                () => new GameDataBuilder().WithResource("Desc_OreIron_C").Build(),
                Throws.InvalidOperationException);
            Assert.That(
                () => Base().WithMilestone("S", 1, [], cost: [("Desc_Nope_C", 1)]).Build(),
                Throws.InvalidOperationException);
            Assert.That(
                () => Base().WithRecipeDefinition("Recipe_A_C", [], []).WithRecipeDefinition("Recipe_A_C", [], []),
                Throws.InvalidOperationException);
            Assert.That(() => new GameDataBuilder().WithNuclearGeneratorRecipes(), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void Schematic_DefaultsAndValues()
    {
        GameData data = Base()
            .WithSchematic("Schematic_A_C", SchematicType.Alternate, unlocks: ["Recipe_A_C"], required: ["Schematic_B_C"], cost: [("Desc_IronIngot_C", 3)])
            .WithMilestone("Schematic_B_C", 4, ["Recipe_B_C"])
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(
                data.Schematics[0],
                Is.EqualTo(data.Schematics[0] with { Id = new SchematicId("Schematic_A_C"), Name = "Schematic_A_C", Type = SchematicType.Alternate, Tier = 0 }));
            Assert.That(data.Schematics[0].Required, Is.EqualTo(ExpectedSequence.Of(new SchematicId("Schematic_B_C"))));
            Assert.That(data.Schematics[0].Cost, Is.EqualTo(ExpectedSequence.Of(new ItemAmount(new ItemId("Desc_IronIngot_C"), 3))));
            Assert.That(data.Schematics[1].Type, Is.EqualTo(SchematicType.Milestone));
            Assert.That(data.Schematics[1].Tier, Is.EqualTo(4));
            Assert.That(data.Schematics[1].UnlocksRecipes, Is.EqualTo(ExpectedSequence.Of(new RecipeId("Recipe_B_C"))));
        });
    }
}
