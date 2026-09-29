using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers.Graph;

namespace Satisfactory.Planner.Tests.Core.Tiers.Graph;

[TestFixture]
public sealed class MachineGroupNodeTests
{
    private static ManufacturingRecipe Recipe() =>
        new(
            new RecipeId("Recipe_Alternate_PureIronIngot_C"),
            "Alternate: Pure Iron Ingot",
            IsAlternate: true,
            TimeSeconds: 12,
            new BuildingId("Desc_OilRefinery_C"),
            [new ItemAmount(new ItemId("Desc_OreIron_C"), 7), new ItemAmount(new ItemId("Desc_Water_C"), 4)],
            [new ItemAmount(new ItemId("Desc_IronIngot_C"), 13)],
            IsVariablePower: false,
            MinPowerMw: 0,
            MaxPowerMw: 0);

    [Test]
    public void Constructor_KeepsTheValuesAndUsesTheRecipeIdAsId()
    {
        ManufacturingRecipe recipe = Recipe();
        var node = new MachineGroupNode(recipe, 3, "Iron Ingot", "Refinery", makesNewItem: true);

        Assert.Multiple(() =>
        {
            Assert.That(node.Id, Is.EqualTo("Recipe_Alternate_PureIronIngot_C"));
            Assert.That(((IGraphNode)node).Id, Is.EqualTo("Recipe_Alternate_PureIronIngot_C"));
            Assert.That(node.ToString(), Is.EqualTo("Recipe_Alternate_PureIronIngot_C"));
            Assert.That(node.Recipe, Is.SameAs(recipe));
            Assert.That(node.NewMachines, Is.EqualTo(3));
            Assert.That(node.ProductName, Is.EqualTo("Iron Ingot"));
            Assert.That(node.BuildingName, Is.EqualTo("Refinery"));
            Assert.That(node.MakesNewItem, Is.True);
            Assert.That(node.Kind, Is.EqualTo(TierGraphNodeKind.Machine));
        });
    }

    [Test]
    public void Step_IsZeroUntilTheBuildOrderSetsIt()
    {
        var node = new MachineGroupNode(Recipe(), 1, "Iron Ingot", "Refinery", makesNewItem: false);

        Assert.Multiple(() =>
        {
            Assert.That(node.Step, Is.Zero);
            Assert.That(node.MakesNewItem, Is.False);
            Assert.That(node.Label, Is.EqualTo("#0 Iron Ingot\n1 x Refinery\nAlternate: Pure Iron Ingot"));
        });
    }

    [Test]
    public void Label_HasStepAndProduct_MachinesAndBuilding_AndRecipeOnThreeLines()
    {
        var node = new MachineGroupNode(Recipe(), 12, "Iron Ingot", "Refinery", makesNewItem: false) { Step = 17 };

        Assert.Multiple(() =>
        {
            Assert.That(node.Step, Is.EqualTo(17));
            Assert.That(node.Label, Is.EqualTo("#17 Iron Ingot\n12 x Refinery\nAlternate: Pure Iron Ingot"));
        });
    }

    [Test]
    public void Label_PrintsLargeCountsWithoutGrouping()
    {
        var node = new MachineGroupNode(Recipe(), 12345, "Iron Ingot", "Refinery", makesNewItem: false);

        Assert.That(node.Label, Does.Contain("\n12345 x Refinery\n"));
    }

    [Test]
    public void Constructor_RejectsNullArguments()
    {
        ManufacturingRecipe recipe = Recipe();
        Assert.Multiple(() =>
        {
            Assert.That(() => new MachineGroupNode(null!, 1, "p", "b", false), Throws.ArgumentNullException);
            Assert.That(() => new MachineGroupNode(recipe, 1, null!, "b", false), Throws.ArgumentNullException);
            Assert.That(() => new MachineGroupNode(recipe, 1, "p", null!, false), Throws.ArgumentNullException);
        });
    }
}
