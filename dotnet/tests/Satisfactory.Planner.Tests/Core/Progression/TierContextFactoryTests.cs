using System.Security.Cryptography;
using System.Text;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Progression;

[TestFixture]
public sealed class TierContextFactoryTests
{
    private static GameData Sample() => new GameDataBuilder()
        .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
        .WithFluid("Desc_Water_C", "Water").WithResource("Desc_Water_C")
        .WithItem("Desc_IronIngot_C", "Iron Ingot")
        .WithItem("Desc_IronPlate_C", "Iron Plate")
        .WithItem("Desc_Concrete_C", "Concrete")
        .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
        .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
        .WithBuilding("Desc_FoundryMk1_C", "Foundry", 16)
        .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
        .WithRecipe("Recipe_IronPlate_C", "Iron Plate", "Desc_ConstructorMk1_C", 6, [("Desc_IronIngot_C", 3)], [("Desc_IronPlate_C", 2)])
        .WithRecipe("Recipe_Wet_C", "Wet Concrete", "Desc_FoundryMk1_C", 6, [("Desc_IronIngot_C", 3), ("Desc_Water_C", 1)], [("Desc_Concrete_C", 2)])
        .WithMilestone("Schematic_0-1_C", 0, ["Recipe_IngotIron_C", "Recipe_IronPlate_C"])
        .WithMilestone("Schematic_2-1_C", 2, ["Recipe_WaterPump_C", "Recipe_Wet_C"])
        .WithMilestone("Schematic_3-1_C", 3, ["Recipe_FoundryMk1_C"])
        .Build();

    [Test]
    public void Create_GatesRecipesAndResourcesByTier()
    {
        GameData data = Sample();
        var factory = new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal);

        ITierContext t0 = factory.Create(0);
        ITierContext t2 = factory.Create(2);
        ITierContext t3 = factory.Create(3);

        Assert.Multiple(() =>
        {
            Assert.That(t0.Tier, Is.Zero);
            Assert.That(t0.Data, Is.SameAs(data));
            Assert.That(t0.LockedRawResources, Is.EquivalentTo(ExpectedSequence.Of(new ItemId("Desc_Water_C"))));
            Assert.That(t0.AvailableRecipes.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C", "Recipe_IronPlate_C")));
            Assert.That(t0.Extractors.IsExtractable(new ItemId("Desc_Water_C")), Is.False);
            Assert.That(t0.Automatable, Does.Not.Contain(new ItemId("Desc_Water_C")));

            Assert.That(t2.LockedRawResources, Is.Empty);
            Assert.That(t2.Extractors.IsExtractable(new ItemId("Desc_Water_C")), Is.True);
            Assert.That(t2.AvailableRecipes, Has.Count.EqualTo(2), "the foundry is tier 3");
            Assert.That(t2.Automatable, Does.Contain(new ItemId("Desc_Water_C")));
            Assert.That(t2.Automatable, Does.Not.Contain(new ItemId("Desc_Concrete_C")));

            Assert.That(t3.AvailableRecipes.Select(r => r.Id.Value), Is.EqualTo(ExpectedSequence.Of("Recipe_IngotIron_C", "Recipe_IronPlate_C", "Recipe_Wet_C")));
            Assert.That(t3.Automatable, Does.Contain(new ItemId("Desc_Concrete_C")));
        });
    }

    [Test]
    public void Create_UsesTheMinerAndPurity()
    {
        GameData data = Sample();
        var factory = new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk2, NodePurity.Impure);

        ITierContext context = factory.Create(0);

        Assert.That(context.Extractors.For(new ItemId("Desc_OreIron_C")), Is.EqualTo(new ExtractorSpec("Miner Mk.2", 60, 15)));
    }

    // ---- the real data file (expected values printed by the Python set_tier) -------------------------------------

    private static TierContextFactory RealFactory()
    {
        GameData data = CoreRealGameData.Data;
        return new TierContextFactory(data, new UnlockTierResolver(data), MinerTier.Mk1, NodePurity.Normal);
    }

    [Test]
    public void RealData_Tier0()
    {
        ITierContext context = RealFactory().Create(0);

        Assert.Multiple(() =>
        {
            Assert.That(
                context.LockedRawResources.Select(c => c.Value).Order(StringComparer.Ordinal),
                Is.EqualTo(ExpectedSequence.Of("Desc_LiquidOil_C", "Desc_NitrogenGas_C", "Desc_Water_C")));
            Assert.That(
                context.AvailableRecipes.Select(r => r.Id.Value),
                Is.EqualTo(ExpectedSequence.Of(
                    "Recipe_IronPlate_C", "Recipe_IronRod_C", "Recipe_IngotIron_C", "Recipe_Alternate_IngotSteel_2_C",
                    "Recipe_Biomass_Leaves_C", "Recipe_Biomass_Wood_C", "Recipe_Concrete_C", "Recipe_Screw_C", "Recipe_Cable_C",
                    "Recipe_Wire_C", "Recipe_IngotCopper_C")));
            Assert.That(
                context.Automatable.Select(c => c.Value).Order(StringComparer.Ordinal),
                Is.EqualTo(ExpectedSequence.Of(
                    "Desc_Cable_C", "Desc_Cement_C", "Desc_Coal_C", "Desc_CopperIngot_C", "Desc_IronIngot_C", "Desc_IronPlate_C",
                    "Desc_IronRod_C", "Desc_IronScrew_C", "Desc_OreBauxite_C", "Desc_OreCopper_C", "Desc_OreGold_C",
                    "Desc_OreIron_C", "Desc_OreUranium_C", "Desc_RawQuartz_C", "Desc_SAM_C", "Desc_Stone_C", "Desc_Sulfur_C",
                    "Desc_Wire_C")));
        });
    }

    [Test]
    public void RealData_Tier9()
    {
        ITierContext context = RealFactory().Create(9);
        string sorted = string.Join("\n", context.Automatable.Select(c => c.Value).Order(StringComparer.Ordinal));
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sorted)));

        Assert.Multiple(() =>
        {
            Assert.That(context.LockedRawResources, Is.Empty);
            Assert.That(context.AvailableRecipes, Has.Count.EqualTo(278));
            Assert.That(context.Automatable, Has.Count.EqualTo(135));
            Assert.That(hash, Is.EqualTo("7a4f3494502d032c0cf15978b67340e0573c7c2e35ab48fd6b85f06ab4a50870"));
            Assert.That(context.Automatable, Does.Contain(new ItemId("Desc_NuclearWaste_C")));
            Assert.That(context.Automatable, Does.Contain(new ItemId("BP_ItemDescriptorPortableMiner_C")));
        });
    }

    [TestCase(0, 11, 18, "Desc_LiquidOil_C,Desc_NitrogenGas_C,Desc_Water_C")]
    [TestCase(1, 30, 24, "Desc_LiquidOil_C,Desc_NitrogenGas_C,Desc_Water_C")]
    [TestCase(2, 57, 35, "Desc_LiquidOil_C,Desc_NitrogenGas_C,Desc_Water_C")]
    [TestCase(3, 77, 41, "Desc_LiquidOil_C,Desc_NitrogenGas_C")]
    [TestCase(4, 83, 45, "Desc_LiquidOil_C,Desc_NitrogenGas_C")]
    [TestCase(5, 141, 64, "Desc_NitrogenGas_C")]
    [TestCase(6, 162, 75, "Desc_NitrogenGas_C")]
    [TestCase(7, 193, 89, "Desc_NitrogenGas_C")]
    [TestCase(8, 234, 116, "")]
    [TestCase(9, 278, 135, "")]
    public void RealData_EveryTier(int tier, int available, int automatable, string locked)
    {
        ITierContext context = RealFactory().Create(tier);

        Assert.Multiple(() =>
        {
            Assert.That(context.AvailableRecipes, Has.Count.EqualTo(available));
            Assert.That(context.Automatable, Has.Count.EqualTo(automatable));
            Assert.That(
                string.Join(",", context.LockedRawResources.Select(c => c.Value).Order(StringComparer.Ordinal)),
                Is.EqualTo(locked));
        });
    }
}
