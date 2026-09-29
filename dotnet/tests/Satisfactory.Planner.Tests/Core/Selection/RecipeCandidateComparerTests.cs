using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Selection;

namespace Satisfactory.Planner.Tests.Core.Selection;

[TestFixture]
public sealed class RecipeCandidateComparerTests
{
    private static readonly ItemId Ore = new("Desc_Ore_C");
    private static readonly ItemId Widget = new("Desc_Widget_C");
    private static readonly ItemId Other = new("Desc_Other_C");

    private static readonly RecipeCandidateComparer Comparer = new(
        Widget,
        "Widget",
        new ResourceCostTable(new OrderedDictionary<ItemId, double> { [Ore] = 1.0 }, new CreditPolicy()));

    private static ManufacturingRecipe Recipe(
        string name, double ore = 1.0, bool alternate = false, int ingredients = 1, bool widgetFirst = true)
    {
        var inputs = Enumerable.Range(0, ingredients).Select(_ => new ItemAmount(Ore, ore / ingredients)).ToList();
        List<ItemAmount> products = widgetFirst
            ? [new(Widget, 1)]
            : [new(Other, 1), new(Widget, 1)];
        return new ManufacturingRecipe(
            new RecipeId("Recipe_" + name), name, alternate, 4, new BuildingId("Desc_ConstructorMk1_C"), inputs, products,
            false, 0, 0);
    }

    private static void AssertFirst(Recipe preferred, Recipe other)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Comparer.Compare(preferred, other), Is.Negative);
            Assert.That(Comparer.Compare(other, preferred), Is.Positive);
        });
    }

    [Test]
    public void CheaperFirst_EvenWhenAlternate() =>
        AssertFirst(Recipe("Zeta", ore: 1.0, alternate: true), Recipe("Alpha", ore: 1.00001));

    [Test]
    public void CostIsRoundedToSixDecimals_ThenVanillaWins() =>
        AssertFirst(Recipe("Zeta", ore: 1.0000001), Recipe("Alpha", ore: 1.0, alternate: true));

    [Test]
    public void VanillaBeforeAlternate() =>
        AssertFirst(Recipe("Zeta"), Recipe("Alpha", alternate: true));

    [Test]
    public void UnpackagingLast() =>
        AssertFirst(Recipe("Zeta"), Recipe("Unpackage Widget"));

    [Test]
    public void MainProductFirst() =>
        AssertFirst(Recipe("Zeta"), Recipe("Alpha", widgetFirst: false));

    [Test]
    public void NamedLikeTheItemFirst_IgnoringCaseAndAlternatePrefix()
    {
        AssertFirst(Recipe("Alternate: WIDGET", alternate: true), Recipe("Alternate: Awesome Widget", alternate: true));
        AssertFirst(Recipe("widget"), Recipe("Awesome"));
    }

    [Test]
    public void FewerIngredientsFirst() =>
        AssertFirst(Recipe("Zeta", ingredients: 1), Recipe("Alpha", ingredients: 2));

    [Test]
    public void NameOrdinalLast() =>
        AssertFirst(Recipe("Zeta"), Recipe("alpha"));

    [Test]
    public void SameRecipe_IsEqual_NullsFirst()
    {
        ManufacturingRecipe recipe = Recipe("Zeta");

        Assert.Multiple(() =>
        {
            Assert.That(Comparer.Compare(recipe, recipe), Is.Zero);
            Assert.That(Comparer.Compare(Recipe("Zeta"), Recipe("Zeta")), Is.Zero);
            Assert.That(Comparer.Compare(null, recipe), Is.Negative);
            Assert.That(Comparer.Compare(recipe, null), Is.Positive);
        });
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        var costs = new ResourceCostTable(new OrderedDictionary<ItemId, double>(), new CreditPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(() => new RecipeCandidateComparer(Widget, null!, costs), Throws.ArgumentNullException);
            Assert.That(() => new RecipeCandidateComparer(Widget, "Widget", null!), Throws.ArgumentNullException);
        });
    }
}
