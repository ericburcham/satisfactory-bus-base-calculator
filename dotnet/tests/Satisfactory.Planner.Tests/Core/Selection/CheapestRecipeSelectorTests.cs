using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Selection;

[TestFixture]
public sealed class CheapestRecipeSelectorTests
{
    private const string Sulfur = "Desc_Sulfur_C";
    private const string CompactedCoal = "Desc_CompactedCoal_C";
    private const string RocketFuel = "Desc_RocketFuel_C";
    private const string Widget = "Desc_Widget_C";
    private const string Residue = "Desc_Residue_C";
    private const string Packaged = "Desc_PackagedWidget_C";
    private const string Locked = "Desc_Locked_C";

    private static List<string> Ids(IEnumerable<Recipe> recipes) => recipes.Select(r => r.Id.Value).ToList();

    /// <summary>
    /// Compacted Coal's cheapest recipe needs Rocket Fuel, whose only recipe needs Compacted Coal: the costs spiral down
    /// to nearly zero, so the cycle must be rejected by the choice.
    /// </summary>
    private static GameData CompactedCoalCycle() => new GameDataBuilder()
        .WithItem(Coal, "Coal").WithResource(Coal)
        .WithItem(Sulfur, "Sulfur").WithResource(Sulfur)
        .WithItem(CompactedCoal, "Compacted Coal")
        .WithItem(RocketFuel, "Rocket Fuel")
        .WithBuilding(Assembler, "Assembler", 15)
        .WithBuilding(Refinery, "Refinery", 30)
        .WithRecipe(
            "Recipe_Alternate_EnrichedCoal_C", "Alternate: Compacted Coal", Assembler, 12,
            [(Coal, 5), (Sulfur, 5)], [(CompactedCoal, 5)], alternate: true)
        .WithRecipe("Recipe_RocketFuel_C", "Rocket Fuel", Refinery, 6, [(CompactedCoal, 1)], [(RocketFuel, 10)])
        .WithRecipe("Recipe_FuelCoal_C", "Compacted Coal from Fuel", Refinery, 6, [(RocketFuel, 1)], [(CompactedCoal, 1)])
        .Build();

    [Test]
    public void Choose_PicksTheCheapestCandidate_ExtractablesGetNone()
    {
        ITierContext context = Context(ModularFrames()
            .WithRecipe(
                "Recipe_Alternate_CastScrew_C", "Alternate: Cast Screw", Constructor, 24,
                [(IronIngot, 5)], [(Screw, 20)], alternate: true)
            .Build());

        RecipeChoice choice = Choice(context);

        Assert.Multiple(() =>
        {
            Assert.That(choice.For(Item(Screw))!.Id.Value, Is.EqualTo("Recipe_Screw_C"), "both cost 0.25 ore: vanilla wins");
            Assert.That(choice.For(Item(IronPlate))!.Id.Value, Is.EqualTo("Recipe_IronPlate_C"));
            Assert.That(choice.For(Item(IronOre)), Is.Null);
            Assert.That(choice.Recipes.Keys, Does.Not.Contain(Item(IronOre)));
        });
    }

    [Test]
    public void Choose_RejectsACandidateThatMakesTheItemDependOnItself()
    {
        ITierContext context = Context(CompactedCoalCycle());
        var credit = new CreditPolicy();
        ResourceCostTable costs = Costs(context, credit);
        var selector = new CheapestRecipeSelector();

        IReadOnlyList<Recipe> candidates = selector.Candidates(context, costs, credit, Item(CompactedCoal));
        RecipeChoice choice = selector.Choose(context, costs, credit);

        Assert.Multiple(() =>
        {
            Assert.That(Ids(candidates), Is.EqualTo(ExpectedSequence.Of("Recipe_FuelCoal_C", "Recipe_Alternate_EnrichedCoal_C")));
            Assert.That(choice.For(Item(CompactedCoal))!.Id.Value, Is.EqualTo("Recipe_Alternate_EnrichedCoal_C"));
            Assert.That(choice.For(Item(RocketFuel))!.Id.Value, Is.EqualTo("Recipe_RocketFuel_C"));
        });
    }

    [Test]
    public void Candidates_SortedCheapestFirst()
    {
        ITierContext context = Context(IronRods()
            .WithRecipe("Recipe_Alternate_RodA_C", "Alternate: Steel Rod", Constructor, 4, [(IronIngot, 2)], [(IronRod, 1)], alternate: true)
            .WithRecipe("Recipe_Alternate_RodB_C", "Alternate: Cheap Rod", Constructor, 4, [(IronIngot, 1)], [(IronRod, 2)], alternate: true)
            .Build());
        var credit = new CreditPolicy();

        IReadOnlyList<Recipe> candidates =
            new CheapestRecipeSelector().Candidates(context, Costs(context, credit), credit, Item(IronRod));

        Assert.That(
            Ids(candidates),
            Is.EqualTo(ExpectedSequence.Of("Recipe_Alternate_RodB_C", "Recipe_IronRod_C", "Recipe_Alternate_RodA_C")));
    }

    [Test]
    public void Candidates_ExcludeUnpackagingForbiddenByproductsAndNonAutomatableIngredients()
    {
        GameData data = IronRods()
            .WithItem(Widget, "Widget")
            .WithFluid(Residue, "Residue")
            .WithItem(Packaged, "Packaged Widget")
            .WithItem(Locked, "Locked Thing")
            .WithRecipe("Recipe_Widget_C", "Widget", Constructor, 4, [(IronIngot, 1)], [(Widget, 1), (Residue, 1)])
            .WithRecipe("Recipe_WidgetClean_C", "Clean Widget", Constructor, 4, [(IronIngot, 3)], [(Widget, 1)])
            .WithRecipe("Recipe_WidgetLocked_C", "Locked Widget", Constructor, 4, [(Locked, 1)], [(Widget, 1)])
            .WithRecipe("Recipe_PackageWidget_C", "Packaged Widget", Constructor, 4, [(Widget, 1)], [(Packaged, 1)])
            .WithRecipe("Recipe_UnpackageWidget_C", "Unpackage Widget", Constructor, 4, [(Packaged, 1)], [(Widget, 1)])
            .Build();
        ITierContext context = Context(data);
        var selector = new CheapestRecipeSelector();
        var credit = new CreditPolicy();
        ResourceCostTable costs = Costs(context, credit);

        IReadOnlyList<Recipe> before = selector.Candidates(context, costs, credit, Item(Widget));
        credit.ForbidByproduct(Item(Residue));
        IReadOnlyList<Recipe> after = selector.Candidates(context, costs, credit, Item(Widget));
        IReadOnlyList<Recipe> residue = selector.Candidates(context, costs, credit, Item(Residue));

        Assert.Multiple(() =>
        {
            Assert.That(Ids(before), Is.EqualTo(ExpectedSequence.Of("Recipe_Widget_C", "Recipe_WidgetClean_C")));
            Assert.That(Ids(after), Is.EqualTo(ExpectedSequence.Of("Recipe_WidgetClean_C")));
            Assert.That(Ids(residue), Is.EqualTo(ExpectedSequence.Of("Recipe_Widget_C")), "the forbidden fluid itself may still be made");
        });
    }

    [Test]
    public void Choose_ItemWithoutCandidatesGetsNoRecipe()
    {
        GameData data = IronRods()
            .WithItem(Widget, "Widget")
            .WithItem(Packaged, "Packaged Widget")
            .WithRecipe("Recipe_PackageRod_C", "Packaged Widget", Constructor, 4, [(IronRod, 1)], [(Packaged, 1)])
            .WithRecipe("Recipe_UnpackageWidget_C", "Unpackage Widget", Constructor, 4, [(Packaged, 1)], [(Widget, 1)])
            .Build();
        ITierContext context = Context(data);

        RecipeChoice choice = Choice(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.Automatable, Does.Contain(Item(Widget)));
            Assert.That(choice.For(Item(Widget)), Is.Null);
            Assert.That(choice.For(Item(Packaged)), Is.Not.Null);
        });
    }

    [Test]
    public void Candidates_OnlyRecipesOfTheTier()
    {
        GameData data = IronRods()
            .WithRecipe("Recipe_Alternate_RodB_C", "Alternate: Cheap Rod", Constructor, 4, [(IronIngot, 1)], [(IronRod, 2)], alternate: true)
            .WithMilestone("Schematic_2_C", 2, ["Recipe_Alternate_RodB_C"])
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(Choice(Context(data, 1)).For(Item(IronRod))!.Id.Value, Is.EqualTo("Recipe_IronRod_C"));
            Assert.That(Choice(Context(data, 2)).For(Item(IronRod))!.Id.Value, Is.EqualTo("Recipe_Alternate_RodB_C"));
        });
    }

    [Test]
    public void NullArguments_Throw()
    {
        ITierContext context = Context(IronRods().Build());
        var credit = new CreditPolicy();
        ResourceCostTable costs = Costs(context, credit);
        var selector = new CheapestRecipeSelector();

        Assert.Multiple(() =>
        {
            Assert.That(() => selector.Choose(null!, costs, credit), Throws.ArgumentNullException);
            Assert.That(() => selector.Choose(context, null!, credit), Throws.ArgumentNullException);
            Assert.That(() => selector.Choose(context, costs, null!), Throws.ArgumentNullException);
            Assert.That(() => selector.Candidates(context, costs, null!, Item(IronRod)), Throws.ArgumentNullException);
        });
    }
}
