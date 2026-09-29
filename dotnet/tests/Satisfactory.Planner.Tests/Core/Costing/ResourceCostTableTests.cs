using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Costing;

[TestFixture]
public sealed class ResourceCostTableTests
{
    private static readonly ItemId Ore = new("Ore");
    private static readonly ItemId Water = new("Water");
    private static readonly ItemId A = new("A");
    private static readonly ItemId B = new("B");
    private static readonly ItemId Unknown = new("Unknown");

    private static ManufacturingRecipe Recipe(IReadOnlyList<ItemAmount> ingredients, IReadOnlyList<ItemAmount> products) =>
        new(new RecipeId("R"), "R", false, 4, new BuildingId("Desc_ConstructorMk1_C"), ingredients, products, false, 0, 0);

    private static ResourceCostTable Table(CreditPolicy credit) => new(
        new OrderedDictionary<ItemId, double> { [Ore] = 1.0, [Water] = 0.001, [A] = 2.0, [B] = 0.5 },
        credit);

    [Test]
    public void CostOf_SumsIngredientsAndDividesByTheProductAmount()
    {
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 3), new(Water, 100)], [new(A, 2)]);

        Assert.That(table.CostOf(recipe, A), Is.EqualTo(((1.0 * 3) + (0.001 * 100)) / 2));
    }

    [Test]
    public void CostOf_CreditsOtherProducts_UnknownOnesCountZero()
    {
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 4)], [new(A, 1), new(B, 2), new(Unknown, 5)]);

        Assert.Multiple(() =>
        {
            Assert.That(table.CostOf(recipe, A), Is.EqualTo((4.0 - (0.5 * 2)) / 1));
            Assert.That(table.CostOf(recipe, B), Is.EqualTo((4.0 - (2.0 * 1)) / 2));
        });
    }

    [Test]
    public void CostOf_NoCredit_ByproductEarnsNothing()
    {
        var credit = new CreditPolicy();
        credit.DenyCredit([B]);
        ResourceCostTable table = Table(credit);
        ManufacturingRecipe recipe = Recipe([new(Ore, 4)], [new(A, 1), new(B, 2)]);

        Assert.That(table.CostOf(recipe, A), Is.EqualTo(4.0));
    }

    [Test]
    public void CostOf_ClampsAtZero()
    {
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 1)], [new(B, 1), new(A, 1)]);

        Assert.That(table.CostOf(recipe, B), Is.Zero);
    }

    [Test]
    public void CostOf_UnknownIngredientIsInfinite()
    {
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 1), new(Unknown, 1)], [new(A, 1), new(B, 1)]);

        Assert.That(table.CostOf(recipe, A), Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void CostOf_SameItemTwice_TheOtherEntryIsADifferentProduct()
    {
        // Python "q is not p": the first entry of A is the product, the second one earns credit.
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 6)], [new(A, 1), new(A, 2)]);

        Assert.That(table.CostOf(recipe, A), Is.EqualTo((6.0 - (2.0 * 2)) / 1));
    }

    [Test]
    public void CostOf_NotAProduct_Throws()
    {
        ResourceCostTable table = Table(new CreditPolicy());
        ManufacturingRecipe recipe = Recipe([new(Ore, 1)], [new(A, 1)]);

        Assert.That(() => table.CostOf(recipe, B), Throws.ArgumentException);
    }

    [Test]
    public void Costs_AreCopiedInOrder_TryGetCost()
    {
        var source = new OrderedDictionary<ItemId, double> { [B] = 0.5, [A] = 2.0 };
        var table = new ResourceCostTable(source, new CreditPolicy());
        source[Ore] = 1.0;

        Assert.Multiple(() =>
        {
            Assert.That(table.Costs.Keys, Is.EqualTo(ExpectedSequence.Of(B, A)));
            Assert.That(table.TryGetCost(A, out double a), Is.True);
            Assert.That(a, Is.EqualTo(2.0));
            Assert.That(table.TryGetCost(Ore, out _), Is.False);
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new ResourceCostTable(null!, new CreditPolicy()), Throws.ArgumentNullException);
            Assert.That(
                () => new ResourceCostTable(new OrderedDictionary<ItemId, double>(), null!), Throws.ArgumentNullException);
        });
    }
}
