using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Progression;

[TestFixture]
public sealed class UnlockTierResolverTests
{
    /// <summary>
    /// A synthetic milestone / MAM chain. The expected tiers were produced by running the Python <c>unlock_tiers</c> on
    /// the same schematics and recipes.
    /// </summary>
    private static GameData Chain() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_IronPlate_C", "Iron Plate")
        .WithItem("Desc_Quartz_C", "Quartz")
        .WithItem("Desc_Gathered_C", "Gathered")
        // Listed first: needs the MAM, which is unknown in the first pass; costs Quartz (tier 4).
        .WithSchematic("Schematic_Research_C", SchematicType.Mam, unlocks: ["Recipe_Alternate_Plate_C"], cost: [("Desc_Quartz_C", 10)])
        .WithSchematic("Schematic_0-1_C", SchematicType.Tutorial, 0, unlocks: ["Recipe_IngotIron_C", "Recipe_ConstructorMk1_C"])
        .WithMilestone("Schematic_2-1_C", 2, ["Recipe_Mam_C"], cost: [("Desc_IronPlate_C", 5)], required: ["Schematic_0-1_C"])
        .WithMilestone("Schematic_4-1_C", 4, ["Recipe_Quartz_C"])
        .WithMilestone("Schematic_1-1_C", 1, ["Recipe_IronPlate_C"], cost: [("Desc_IronPlate_C", 50)])
        .WithMilestone("Schematic_3-1_C", 3, ["Recipe_Never_C"], required: ["Schematic_Missing_C"])
        .WithSchematic("Schematic_HD_C", SchematicType.HardDrive, unlocks: ["Recipe_Alternate_Ingot_C"], cost: [("Desc_Gathered_C", 1)])
        .WithSchematic("Schematic_Sink_C", SchematicType.ResourceSink, unlocks: ["Recipe_Sink_C"])
        .WithMilestone("Schematic_5-1_C", 5, ["Recipe_IngotIron_C"])
        .WithMilestone("Schematic_3-2_C", 3, ["Recipe_SmelterMk1_C"])
        .WithRecipeDefinition("Recipe_IngotIron_C", [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipeDefinition("Recipe_IronPlate_C", [("Desc_IronIngot_C", 3)], [("Desc_IronPlate_C", 2)])
        .WithRecipeDefinition("Recipe_Quartz_C", [("Desc_OreIron_C", 1)], [("Desc_Quartz_C", 1)])
        .WithRecipeDefinition("Recipe_Alternate_Plate_C", [("Desc_IronIngot_C", 2)], [("Desc_IronPlate_C", 3)])
        .WithRecipeDefinition("Recipe_Alternate_Ingot_C", [("Desc_OreIron_C", 2)], [("Desc_IronIngot_C", 3)])
        .WithBuildRecipe("Desc_ConstructorMk1_C")
        .WithBuildRecipe("Desc_Mam_C", [("Desc_IronPlate_C", 10)])
        .WithBuildRecipe("Desc_SmelterMk1_C")
        .Build();

    [Test]
    public void RecipeUnlockTiers_MatchPythonIncludingInsertionOrder()
    {
        var resolver = new UnlockTierResolver(Chain());

        Assert.That(
            resolver.RecipeUnlockTiers.Select(kv => (kv.Key.Value, kv.Value)),
            Is.EqualTo(ExpectedSequence.Of(
                ("Recipe_IngotIron_C", 0),
                ("Recipe_ConstructorMk1_C", 0),
                ("Recipe_Mam_C", 2),
                ("Recipe_Quartz_C", 4),
                ("Recipe_IronPlate_C", 1),
                ("Recipe_Alternate_Ingot_C", 2),
                ("Recipe_SmelterMk1_C", 0),         // TIER_OVERRIDES: the milestone says 3, position kept
                ("Recipe_Alternate_Plate_C", 4)))); // MAM tier 2, but its Quartz cost is tier 4
    }

    [Test]
    public void SchematicsWithUnresolvedRequirementsOrIgnoredTypes_UnlockNothing()
    {
        var resolver = new UnlockTierResolver(Chain());

        Assert.Multiple(() =>
        {
            Assert.That(resolver.RecipeUnlockTiers.ContainsKey(new RecipeId("Recipe_Never_C")), Is.False);
            Assert.That(resolver.RecipeUnlockTiers.ContainsKey(new RecipeId("Recipe_Sink_C")), Is.False);
        });
    }

    [Test]
    public void MamSchematics_NeedTheMam()
    {
        GameData data = new GameDataBuilder()
            .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
            .WithSchematic("Schematic_Research_C", SchematicType.Mam, unlocks: ["Recipe_A_C"])
            .WithSchematic("Schematic_Alternate_C", SchematicType.Alternate, unlocks: ["Recipe_B_C"])
            .WithMilestone("Schematic_1-1_C", 1, ["Recipe_C_C"])
            .Build();

        var resolver = new UnlockTierResolver(data);

        Assert.That(
            resolver.RecipeUnlockTiers.Keys.Select(k => k.Value),
            Is.EqualTo(ExpectedSequence.Of("Recipe_C_C", "Recipe_SmelterMk1_C")));
    }

    [Test]
    public void Overrides_AreAppendedWhenNoSchematicUnlocksTheSmelter()
    {
        var resolver = new UnlockTierResolver(new GameDataBuilder().Build());

        Assert.That(
            resolver.RecipeUnlockTiers.Select(kv => (kv.Key.Value, kv.Value)),
            Is.EqualTo(ExpectedSequence.Of(("Recipe_SmelterMk1_C", 0))));
    }

    [Test]
    public void BuildingTier_UsesTheBuildRecipeAndPythonSliceSemantics()
    {
        GameData data = new GameDataBuilder()
            .WithMilestone("Schematic_2-1_C", 2, ["Recipe_Mam_C"])
            .WithMilestone("Schematic_7-1_C", 7, ["Recipe_"])
            .Build();
        var resolver = new UnlockTierResolver(data);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.BuildingTier(new BuildingId("Desc_Mam_C")), Is.EqualTo(2));
            Assert.That(resolver.BuildingTier(new BuildingId("Xesc_Mam_C")), Is.EqualTo(2), "bcls[5:] does not check the prefix");
            Assert.That(resolver.BuildingTier(new BuildingId("Desc_")), Is.EqualTo(7), "Recipe_ + ''");
            Assert.That(resolver.BuildingTier(new BuildingId("Desc")), Is.EqualTo(7), "a slice past the end is empty");
            Assert.That(resolver.BuildingTier(new BuildingId("")), Is.EqualTo(7));
            Assert.That(resolver.BuildingTier(new BuildingId("Desc_Unknown_C")), Is.Zero);
            Assert.That(resolver.BuildingTier(new BuildingId("Desc_SmelterMk1_C")), Is.Zero, "override");
        });
    }

    [Test]
    public void RecipeTier_IsTheLaterOfOwnAndBuildingTier()
    {
        GameData data = new GameDataBuilder()
            .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
            .WithItem("Desc_IronIngot_C", "Iron Ingot")
            .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
            .WithBuilding("Desc_Mam_C", "MAM", 0)
            .WithMilestone("Schematic_2-1_C", 2, ["Recipe_Mam_C"])
            .WithMilestone("Schematic_4-1_C", 4, ["Recipe_Late_C"])
            .WithMilestone("Schematic_1-1_C", 1, ["Recipe_Early_C"])
            .WithRecipe("Recipe_Late_C", "Late", "Desc_ConstructorMk1_C", 1, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
            .WithRecipe("Recipe_Early_C", "Early", "Desc_Mam_C", 1, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
            .WithRecipe("Recipe_Unlisted_C", "Unlisted", "Desc_ConstructorMk1_C", 1, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
            .Build();
        var resolver = new UnlockTierResolver(data);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.RecipeTier(data.Recipes[0]), Is.EqualTo(4), "own tier 4, building tier 0");
            Assert.That(resolver.RecipeTier(data.Recipes[1]), Is.EqualTo(2), "own tier 1, MAM tier 2");
            Assert.That(resolver.RecipeTier(data.Recipes[2]), Is.Zero, "not unlocked by any schematic");
        });
    }

    [Test]
    public void RawResourceTier_UsesTheExtractorBuilding()
    {
        GameData data = new GameDataBuilder()
            .WithMilestone("Schematic_3-1_C", 3, ["Recipe_WaterPump_C"])
            .WithMilestone("Schematic_5-1_C", 5, ["Recipe_OilPump_C"])
            .WithMilestone("Schematic_8-1_C", 8, ["Recipe_FrackingSmasher_C"])
            .WithMilestone("Schematic_1-1_C", 1, ["Recipe_MinerMk1_C"])
            .Build();
        var resolver = new UnlockTierResolver(data);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.RawResourceTier(new ItemId("Desc_Water_C")), Is.EqualTo(3));
            Assert.That(resolver.RawResourceTier(new ItemId("Desc_LiquidOil_C")), Is.EqualTo(5));
            Assert.That(resolver.RawResourceTier(new ItemId("Desc_NitrogenGas_C")), Is.EqualTo(8));
            Assert.That(resolver.RawResourceTier(new ItemId("Desc_OreIron_C")), Is.EqualTo(1));
            Assert.That(resolver.RawResourceTier(new ItemId("Desc_Anything_C")), Is.EqualTo(1));
        });
    }

    // ---- the real data file (expected values printed by the Python) ---------------------------------------------

    [Test]
    public void RealData_RecipeUnlockTiersEqualPythonRecTier()
    {
        var resolver = new UnlockTierResolver(CoreRealGameData.Data);
        IReadOnlyDictionary<RecipeId, int> tiers = resolver.RecipeUnlockTiers;
        var text = new StringBuilder();
        foreach ((RecipeId id, int tier) in tiers)
        {
            text.Append(CultureInfo.InvariantCulture, $"{id}={tier}\n");
        }

        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));

        Assert.Multiple(() =>
        {
            Assert.That(tiers, Has.Count.EqualTo(366));
            Assert.That(
                tiers.Take(10).Select(kv => (kv.Key.Value, kv.Value)),
                Is.EqualTo(ExpectedSequence.Of(
                    ("Recipe_ResourceSink_C", 2), ("Recipe_ResourceSinkShop_C", 2), ("Recipe_GeneratorCoal_C", 3),
                    ("Recipe_WaterPump_C", 3), ("Recipe_Pipeline_C", 3), ("Recipe_PipeSupport_C", 3),
                    ("Recipe_PipelineJunction_Cross_C", 3), ("Recipe_PipelinePump_C", 3), ("Recipe_PipeStorageTank_C", 3),
                    ("Recipe_QuantumEncoder_C", 9))));
            Assert.That(
                tiers.TakeLast(5).Select(kv => (kv.Key.Value, kv.Value)),
                Is.EqualTo(ExpectedSequence.Of(
                    ("Recipe_Protein_Hog_C", 1), ("Recipe_Protein_Spitter_C", 1), ("Recipe_Biomass_Mycelia_C", 1),
                    ("Recipe_PowerCrystalShard_1_C", 1), ("Recipe_Gunpowder_C", 1))));
            Assert.That(hash, Is.EqualTo("93cfd603c18c5156f7880b14a9126a52d0c38cd6c07257d36fe9ea9e9c576ad5"));
        });
    }

    [TestCase("Recipe_SmelterMk1_C", 0)]
    [TestCase("Recipe_Mam_C", 1)]
    [TestCase("Recipe_Alternate_SteelRod_C", 3)]
    [TestCase("Recipe_Alternate_PureIronIngot_C", 5)]
    [TestCase("Recipe_Plastic_C", 5)]
    [TestCase("Recipe_Silica_C", 1)]
    [TestCase("Recipe_GeneratorNuclear_C", 8)]
    [TestCase("Recipe_ConstructorMk1_C", 0)]
    [TestCase("Recipe_OilPump_C", 5)]
    [TestCase("Recipe_WaterPump_C", 3)]
    [TestCase("Recipe_FrackingSmasher_C", 8)]
    [TestCase("Recipe_MinerMk1_C", 0)]
    [TestCase("Recipe_Alternate_Wire_1_C", 1)]
    [TestCase("Recipe_Biomass_Leaves_C", 0)]
    public void RealData_RecipeUnlockTier(string recipe, int expected)
    {
        var resolver = new UnlockTierResolver(CoreRealGameData.Data);

        Assert.That(resolver.RecipeUnlockTiers[new RecipeId(recipe)], Is.EqualTo(expected));
    }

    [TestCase("Recipe_IronPlate_C")]
    [TestCase("Recipe_FoundryMk1_C")]
    public void RealData_RecipesNoSchematicUnlocks_AreAbsent(string recipe)
    {
        var resolver = new UnlockTierResolver(CoreRealGameData.Data);

        Assert.That(resolver.RecipeUnlockTiers.ContainsKey(new RecipeId(recipe)), Is.False);
    }

    [Test]
    public void RealData_RecipeAndRawTiers()
    {
        GameData data = CoreRealGameData.Data;
        var resolver = new UnlockTierResolver(data);
        Recipe RecipeOf(string id) => data.Recipes.Single(r => r.Id.Value == id);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.RecipeTier(RecipeOf("Recipe_IronPlate_C")), Is.Zero);
            Assert.That(resolver.RecipeTier(RecipeOf("Recipe_Plastic_C")), Is.EqualTo(5));
            Assert.That(resolver.RecipeTier(RecipeOf("Recipe_Alternate_PureIronIngot_C")), Is.EqualTo(5));
            Assert.That(resolver.RecipeTier(RecipeOf("Gen_Nuclear_Uranium")), Is.EqualTo(8));
            Assert.That(resolver.RecipeTier(RecipeOf("Gen_Nuclear_Plutonium")), Is.EqualTo(8));
            Assert.That(
                data.Resources.Select(r => (r.Value, resolver.RawResourceTier(r))),
                Is.EqualTo(ExpectedSequence.Of(
                    ("Desc_OreIron_C", 0), ("Desc_Coal_C", 0), ("Desc_NitrogenGas_C", 8), ("Desc_Sulfur_C", 0),
                    ("Desc_Water_C", 3), ("Desc_SAM_C", 0), ("Desc_OreBauxite_C", 0), ("Desc_OreGold_C", 0),
                    ("Desc_OreCopper_C", 0), ("Desc_RawQuartz_C", 0), ("Desc_Stone_C", 0), ("Desc_OreUranium_C", 0),
                    ("Desc_LiquidOil_C", 5))));
        });
    }

    [Test]
    public void RealData_NumberOfRecipesPerTier()
    {
        GameData data = CoreRealGameData.Data;
        var resolver = new UnlockTierResolver(data);

        var counts = data.Recipes.GroupBy(resolver.RecipeTier).OrderBy(g => g.Key).Select(g => (g.Key, g.Count()));

        Assert.That(
            counts,
            Is.EqualTo(ExpectedSequence.Of((0, 11), (1, 19), (2, 27), (3, 20), (4, 6), (5, 58), (6, 21), (7, 31), (8, 41), (9, 44))));
    }
}
