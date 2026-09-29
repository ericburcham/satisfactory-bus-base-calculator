using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Tests.Core.Extraction;

[TestFixture]
public sealed class ExtractorCatalogTests
{
    private static readonly HashSet<ItemId> NoneLocked = [];

    [TestCase(MinerTier.Mk1, NodePurity.Impure, "Miner Mk.1", 30.0, 5.0)]
    [TestCase(MinerTier.Mk1, NodePurity.Normal, "Miner Mk.1", 60.0, 5.0)]
    [TestCase(MinerTier.Mk1, NodePurity.Pure, "Miner Mk.1", 120.0, 5.0)]
    [TestCase(MinerTier.Mk2, NodePurity.Normal, "Miner Mk.2", 120.0, 15.0)]
    [TestCase(MinerTier.Mk2, NodePurity.Pure, "Miner Mk.2", 240.0, 15.0)]
    [TestCase(MinerTier.Mk3, NodePurity.Impure, "Miner Mk.3", 120.0, 45.0)]
    [TestCase(MinerTier.Mk3, NodePurity.Pure, "Miner Mk.3", 480.0, 45.0)]
    public void SolidResource_UsesTheMinerAtThePurity(MinerTier miner, NodePurity purity, string name, double rate, double mw)
    {
        var catalog = new ExtractorCatalog(miner, purity, NoneLocked);

        Assert.That(catalog.For(new ItemId("Desc_OreIron_C")), Is.EqualTo(new ExtractorSpec(name, rate, mw)));
    }

    [TestCase("Desc_OreIron_C")]
    [TestCase("Desc_Coal_C")]
    [TestCase("Desc_Sulfur_C")]
    [TestCase("Desc_SAM_C")]
    [TestCase("Desc_OreBauxite_C")]
    [TestCase("Desc_OreGold_C")]
    [TestCase("Desc_OreCopper_C")]
    [TestCase("Desc_RawQuartz_C")]
    [TestCase("Desc_Stone_C")]
    [TestCase("Desc_OreUranium_C")]
    public void EverySolidResource_IsMined(string resource)
    {
        var catalog = new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, NoneLocked);

        Assert.That(catalog.For(new ItemId(resource))?.Name, Is.EqualTo("Miner Mk.1"));
    }

    [Test]
    public void Fluids_UseTheirFixedExtractorsWhateverTheMinerAndPurity()
    {
        var catalog = new ExtractorCatalog(MinerTier.Mk3, NodePurity.Pure, NoneLocked);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.For(new ItemId("Desc_Water_C")), Is.EqualTo(new ExtractorSpec("Water Extractor", 120, 20)));
            Assert.That(catalog.For(new ItemId("Desc_LiquidOil_C")), Is.EqualTo(new ExtractorSpec("Oil Extractor", 120, 40)));
            Assert.That(
                catalog.For(new ItemId("Desc_NitrogenGas_C")),
                Is.EqualTo(new ExtractorSpec("Resource Well (Nitrogen, normal satellite)", 60, 0)));
        });
    }

    [Test]
    public void OtherItems_HaveNoExtractor()
    {
        var catalog = new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, NoneLocked);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.For(new ItemId("Desc_IronIngot_C")), Is.Null);
            Assert.That(catalog.IsExtractable(new ItemId("Desc_IronIngot_C")), Is.False);
            Assert.That(catalog.IsExtractable(new ItemId("Desc_OreIron_C")), Is.True);
        });
    }

    [Test]
    public void LockedResources_HaveNoExtractor()
    {
        var locked = new HashSet<ItemId> { new("Desc_Water_C"), new("Desc_Coal_C") };
        var catalog = new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, locked);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.For(new ItemId("Desc_Water_C")), Is.Null);
            Assert.That(catalog.For(new ItemId("Desc_Coal_C")), Is.Null);
            Assert.That(catalog.IsExtractable(new ItemId("Desc_Water_C")), Is.False);
            Assert.That(catalog.IsExtractable(new ItemId("Desc_OreIron_C")), Is.True);
            Assert.That(catalog.IsExtractable(new ItemId("Desc_LiquidOil_C")), Is.True);
        });
    }

    [Test]
    public void LockedSet_IsCopied()
    {
        var locked = new HashSet<ItemId>();
        var catalog = new ExtractorCatalog(MinerTier.Mk1, NodePurity.Normal, locked);

        locked.Add(new ItemId("Desc_OreIron_C"));

        Assert.That(catalog.IsExtractable(new ItemId("Desc_OreIron_C")), Is.True);
    }

    [Test]
    public void UndefinedEnumValues_AreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new ExtractorCatalog((MinerTier)42, NodePurity.Normal, NoneLocked),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new ExtractorCatalog(MinerTier.Mk1, (NodePurity)42, NoneLocked),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
