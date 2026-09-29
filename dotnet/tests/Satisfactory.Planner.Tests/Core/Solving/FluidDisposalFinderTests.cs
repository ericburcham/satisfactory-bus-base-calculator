using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Core.Solving.PlanningTestKit;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class FluidDisposalFinderTests
{
    private const string Residue = "Desc_Residue_C";
    private const string Canister = "Desc_FluidCanister_C";
    private const string Solid = "Desc_Solid_C";
    private const string OtherFluid = "Desc_OtherFluid_C";
    private const string Waste = "Desc_NuclearWaste_C";
    private const string Locked = "Desc_Locked_C";
    private const string Packager = "Desc_Packager_C";

    /// <summary>Ore, water, a residue fluid made from ore, canisters and a solid; plus the given disposal recipes.</summary>
    private static GameDataBuilder Base() => new GameDataBuilder()
        .WithItem(IronOre, "Iron Ore").WithResource(IronOre)
        .WithFluid(Water, "Water").WithResource(Water)
        .WithFluid(Residue, "Residue")
        .WithFluid(OtherFluid, "Other Fluid")
        .WithItem(Canister, "Empty Canister")
        .WithItem(Solid, "Solid")
        .WithItem(Waste, "Uranium Waste")
        .WithItem(Locked, "Locked")
        .WithBuilding(Refinery, "Refinery", 30)
        .WithBuilding(Packager, "Packager", 10)
        .WithRecipe("Recipe_Residue_C", "Residue", Refinery, 6, [(IronOre, 1)], [(Residue, 1)])
        .WithRecipe("Recipe_Canister_C", "Empty Canister", Refinery, 6, [(IronOre, 2)], [(Canister, 1)]);

    private static Recipe Find(GameData data, string fluid = Residue)
    {
        ITierContext context = Context(data);
        return new FluidDisposalFinder(context, Costs(context)).Find(Item(fluid));
    }

    [Test]
    public void Find_SingleInputBeforePackaging()
    {
        GameData data = Base()
            .WithRecipe("Recipe_PackagedResidue_C", "Packaged Residue", Packager, 2, [(Residue, 2), (Canister, 2)], [(Solid, 2)])
            .WithRecipe("Recipe_Coke_C", "Zeta Coke", Refinery, 6, [(Residue, 4)], [(Solid, 12)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_Coke_C"));
    }

    [Test]
    public void Find_PackagingBeforeOtherRecipes()
    {
        GameData data = Base()
            .WithRecipe("Recipe_Coke_C", "Coke", Refinery, 6, [(Residue, 4)], [(Solid, 12)])
            .WithRecipe("Recipe_PackagedResidue_C", "Packaged Residue", Packager, 2, [(Residue, 2)], [(Solid, 2)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_PackagedResidue_C"));
    }

    [Test]
    public void Find_FewestExtraResourcesNext()
    {
        GameData data = Base()
            .WithRecipe("Recipe_A_C", "A Dear", Refinery, 6, [(Residue, 1), (Canister, 3)], [(Solid, 1)])
            .WithRecipe("Recipe_B_C", "B Cheap", Refinery, 6, [(Residue, 1), (IronOre, 1)], [(Solid, 1)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_B_C"));
    }

    [Test]
    public void Find_VanillaBeforeAlternate_ThenNameOrdinal()
    {
        GameData alternate = Base()
            .WithRecipe("Recipe_A_C", "Alpha", Refinery, 6, [(Residue, 1)], [(Solid, 1)], alternate: true)
            .WithRecipe("Recipe_B_C", "Zeta", Refinery, 6, [(Residue, 1)], [(Solid, 1)])
            .Build();
        GameData names = Base()
            .WithRecipe("Recipe_A_C", "alpha", Refinery, 6, [(Residue, 1)], [(Solid, 1)])
            .WithRecipe("Recipe_B_C", "Zeta", Refinery, 6, [(Residue, 1)], [(Solid, 1)])
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(Find(alternate).Id.Value, Is.EqualTo("Recipe_B_C"));
            Assert.That(Find(names).Id.Value, Is.EqualTo("Recipe_B_C"), "ordinal: 'Z' sorts before 'a'");
        });
    }

    [Test]
    public void Find_FullTie_FirstRecipeWins()
    {
        GameData data = Base()
            .WithRecipe("Recipe_B_C", "Same", Refinery, 6, [(Residue, 1)], [(Solid, 1)])
            .WithRecipe("Recipe_A_C", "Same", Refinery, 6, [(Residue, 1)], [(Solid, 1)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_B_C"));
    }

    [Test]
    public void Find_WaterMayComeOut()
    {
        GameData data = Base()
            .WithRecipe("Recipe_Wet_C", "Wet Coke", Refinery, 6, [(Residue, 1)], [(Solid, 1), (Water, 1)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_Wet_C"));
    }

    [Test]
    public void Find_ExcludesFluidOrUnsinkableProducts_LockedIngredients_AndGenerators()
    {
        GameData data = Base()
            .WithBuilding("Desc_GeneratorNuclear_C", "Nuclear Power Plant", null)
            .WithRecipe("Recipe_ToFluid_C", "A To Fluid", Refinery, 6, [(Residue, 1)], [(OtherFluid, 1)])
            .WithRecipe("Recipe_ToWaste_C", "A To Waste", Refinery, 6, [(Residue, 1)], [(Waste, 1)])
            .WithRecipe("Recipe_Locked_C", "A Locked", Refinery, 6, [(Residue, 1), (Locked, 1)], [(Solid, 1)])
            .WithGeneratorRecipe("Gen_Residue", "A Burn Residue", "Desc_GeneratorNuclear_C", "Nuclear Power Plant", 6, [(Residue, 1)], [(Solid, 1)], 2500)
            .WithRecipe("Recipe_Good_C", "Z Good", Refinery, 6, [(Residue, 1), (Canister, 1)], [(Solid, 1)])
            .Build();

        Assert.That(Find(data).Id.Value, Is.EqualTo("Recipe_Good_C"));
    }

    [Test]
    public void Find_NoCandidate_ThrowsUndisposable()
    {
        GameData data = Base().Build();

        Assert.That(
            () => Find(data),
            Throws.TypeOf<UndisposableFluidException>().With.Property(nameof(UndisposableFluidException.Fluid)).EqualTo(Item(Residue)));
    }

    [Test]
    public void Constructor_NullArguments_Throw()
    {
        ITierContext context = Context(Base().Build());
        ResourceCostTable costs = Costs(context);

        Assert.Multiple(() =>
        {
            Assert.That(() => new FluidDisposalFinder(null!, costs), Throws.ArgumentNullException);
            Assert.That(() => new FluidDisposalFinder(context, null!), Throws.ArgumentNullException);
        });
    }
}
