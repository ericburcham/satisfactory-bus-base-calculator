using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Configuration;

[TestFixture]
public sealed class PlannerConstantsTests
{
    [Test]
    public void DataLocations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlannerConstants.DataUrl, Is.EqualTo("https://raw.githubusercontent.com/greeny/SatisfactoryTools/dev/data/data.json"));
            Assert.That(PlannerConstants.DataFileName, Is.EqualTo("satisfactory_data.json"));
            Assert.That(PlannerConstants.DefaultDataPath, Is.EqualTo("data/satisfactory_data.json"));
            Assert.That(PlannerConstants.DefaultOutputFolder, Is.EqualTo("tiers"));
        });
    }

    [Test]
    public void SecondsPerMinute_IsSixty() => Assert.That(PlannerConstants.SecondsPerMinute, Is.EqualTo(60));

    [Test]
    public void Extractors_MatchThePython()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlannerConstants.Miners[MinerTier.Mk1], Is.EqualTo(new ExtractorSpec("Miner Mk.1", 60, 5)));
            Assert.That(PlannerConstants.Miners[MinerTier.Mk2], Is.EqualTo(new ExtractorSpec("Miner Mk.2", 120, 15)));
            Assert.That(PlannerConstants.Miners[MinerTier.Mk3], Is.EqualTo(new ExtractorSpec("Miner Mk.3", 240, 45)));
            Assert.That(PlannerConstants.PurityMultipliers[NodePurity.Impure], Is.EqualTo(0.5));
            Assert.That(PlannerConstants.PurityMultipliers[NodePurity.Normal], Is.EqualTo(1.0));
            Assert.That(PlannerConstants.PurityMultipliers[NodePurity.Pure], Is.EqualTo(2.0));
            Assert.That(PlannerConstants.WaterExtractor, Is.EqualTo(new ExtractorSpec("Water Extractor", 120, 20)));
            Assert.That(PlannerConstants.OilExtractor, Is.EqualTo(new ExtractorSpec("Oil Extractor", 120, 40)));
            Assert.That(
                PlannerConstants.NitrogenWell,
                Is.EqualTo(new ExtractorSpec("Resource Well (Nitrogen, normal satellite)", 60, 0)));
        });
    }

    [Test]
    public void WellKnownIds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlannerConstants.Water.Value, Is.EqualTo("Desc_Water_C"));
            Assert.That(PlannerConstants.CrudeOil.Value, Is.EqualTo("Desc_LiquidOil_C"));
            Assert.That(PlannerConstants.NitrogenGas.Value, Is.EqualTo("Desc_NitrogenGas_C"));
            Assert.That(PlannerConstants.NuclearFuelRod.Value, Is.EqualTo("Desc_NuclearFuelRod_C"));
            Assert.That(PlannerConstants.PlutoniumFuelRod.Value, Is.EqualTo("Desc_PlutoniumFuelRod_C"));
            Assert.That(PlannerConstants.NuclearWaste.Value, Is.EqualTo("Desc_NuclearWaste_C"));
            Assert.That(PlannerConstants.PlutoniumWaste.Value, Is.EqualTo("Desc_PlutoniumWaste_C"));
            Assert.That(PlannerConstants.NuclearPowerPlant.Value, Is.EqualTo("Desc_GeneratorNuclear_C"));
            Assert.That(PlannerConstants.NuclearPowerPlantName, Is.EqualTo("Nuclear Power Plant"));
            Assert.That(PlannerConstants.MamRecipe.Value, Is.EqualTo("Recipe_Mam_C"));
            Assert.That(PlannerConstants.DefaultExtractorBuilding.Value, Is.EqualTo("Desc_MinerMk1_C"));
            Assert.That(PlannerConstants.NuclearMw, Is.EqualTo(2500));
            Assert.That(PlannerConstants.NuclearWaterPerMinute, Is.EqualTo(240));
        });
    }

    [Test]
    public void Collections_MatchThePython()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PlannerConstants.Unsinkable,
                Is.EquivalentTo(ExpectedSequence.Of(new ItemId("Desc_NuclearWaste_C"), new ItemId("Desc_PlutoniumWaste_C"))));
            Assert.That(PlannerConstants.ResourceWeights, Has.Count.EqualTo(1));
            Assert.That(PlannerConstants.ResourceWeights[new ItemId("Desc_Water_C")], Is.EqualTo(0.001));
            Assert.That(PlannerConstants.DefaultResourceWeight, Is.EqualTo(1.0));
            Assert.That(
                PlannerConstants.SolidResources.Select(r => r.Value),
                Is.EquivalentTo(ExpectedSequence.Of(
                    "Desc_OreIron_C", "Desc_Coal_C", "Desc_Sulfur_C", "Desc_SAM_C", "Desc_OreBauxite_C", "Desc_OreGold_C",
                    "Desc_OreCopper_C", "Desc_RawQuartz_C", "Desc_Stone_C", "Desc_OreUranium_C")));
            Assert.That(PlannerConstants.ExtractorBuildings, Has.Count.EqualTo(3));
            Assert.That(PlannerConstants.ExtractorBuildings[new ItemId("Desc_Water_C")].Value, Is.EqualTo("Desc_WaterPump_C"));
            Assert.That(PlannerConstants.ExtractorBuildings[new ItemId("Desc_LiquidOil_C")].Value, Is.EqualTo("Desc_OilPump_C"));
            Assert.That(
                PlannerConstants.ExtractorBuildings[new ItemId("Desc_NitrogenGas_C")].Value, Is.EqualTo("Desc_FrackingSmasher_C"));
            Assert.That(PlannerConstants.TierOverrides, Has.Count.EqualTo(1));
            Assert.That(PlannerConstants.TierOverrides[new RecipeId("Recipe_SmelterMk1_C")], Is.Zero);
        });
    }

    [Test]
    public void Tiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlannerConstants.TierNames, Has.Count.EqualTo(10));
            Assert.That(PlannerConstants.TierNames[0], Is.EqualTo("Tier 0 - Onboarding"));
            for (int t = 1; t <= 9; t++)
            {
                Assert.That(PlannerConstants.TierNames[t], Is.EqualTo($"Tier {t}"));
            }

            Assert.That(PlannerConstants.FirstTier, Is.Zero);
            Assert.That(PlannerConstants.LastTier, Is.EqualTo(9));
            Assert.That(PlannerConstants.UnknownItemTier, Is.EqualTo(99));
        });
    }

    [Test]
    public void MarkersTolerancesAndCaps()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PlannerConstants.BuildingDescriptorPrefix, Is.EqualTo("Desc_"));
            Assert.That(PlannerConstants.BuildingRecipePrefix, Is.EqualTo("Recipe_"));
            Assert.That(PlannerConstants.UnpackagePrefix, Is.EqualTo("Unpackage"));
            Assert.That(PlannerConstants.PackagedPrefix, Is.EqualTo("Packaged"));
            Assert.That(PlannerConstants.AlternateNamePrefix, Is.EqualTo("alternate: "));
            Assert.That(PlannerConstants.IngotMarker, Is.EqualTo("Ingot"));
            Assert.That(PlannerConstants.LeftoverTolerance, Is.EqualTo(0.01));
            Assert.That(PlannerConstants.CostImprovementTolerance, Is.EqualTo(1e-9));
            Assert.That(PlannerConstants.ShortfallTolerance, Is.EqualTo(1e-9));
            Assert.That(PlannerConstants.CapacityTolerance, Is.EqualTo(1e-12));
            Assert.That(PlannerConstants.TrimSurplusTolerance, Is.EqualTo(1e-6));
            Assert.That(PlannerConstants.TrimCutTolerance, Is.EqualTo(1e-9));
            Assert.That(PlannerConstants.NewMachineTolerance, Is.EqualTo(1e-6));
            Assert.That(PlannerConstants.ReusedGainTolerance, Is.EqualTo(1e-6));
            Assert.That(PlannerConstants.CostRoundingDigits, Is.EqualTo(6));
            Assert.That(PlannerConstants.PowerRoundingDigits, Is.EqualTo(1));
            Assert.That(PlannerConstants.CostRelaxationPasses, Is.EqualTo(500));
            Assert.That(PlannerConstants.CycleRejectionPasses, Is.EqualTo(100));
            Assert.That(PlannerConstants.ConvergeIterations, Is.EqualTo(5000));
            Assert.That(PlannerConstants.TrimIterations, Is.EqualTo(50));
            Assert.That(PlannerConstants.DisposalIterations, Is.EqualTo(100));
            Assert.That(PlannerConstants.OptimiseRounds, Is.EqualTo(20));
            Assert.That(PlannerConstants.UnlockTierPasses, Is.EqualTo(50));
        });
    }
}
