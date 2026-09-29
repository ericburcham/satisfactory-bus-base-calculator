using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Solving;

[TestFixture]
public sealed class ExistingCapacityTests
{
    private static ManufacturingRecipe Recipe(string id) => new(
        new RecipeId(id), id, false, 4, new BuildingId("B"), [], [new ItemAmount(new ItemId("Desc_" + id), 1)], false, 0, 0);

    [Test]
    public void NewCapacity_IsEmpty()
    {
        var existing = new ExistingCapacity();

        Assert.Multiple(() =>
        {
            Assert.That(existing.Machines, Is.Empty);
            Assert.That(existing.Snapshot(), Is.Empty);
            Assert.That(existing.MachinesOf(new RecipeId("Rod")), Is.Zero);
        });
    }

    [Test]
    public void Add_AccumulatesAndKeepsTheFirstPosition()
    {
        var existing = new ExistingCapacity();
        ManufacturingRecipe rod = Recipe("Rod");
        ManufacturingRecipe screw = Recipe("Screw");

        existing.Add(rod, 2);
        existing.Add(screw, 1);
        existing.Add(rod, 1);

        Assert.Multiple(() =>
        {
            Assert.That(existing.MachinesOf(rod.Id), Is.EqualTo(3));
            Assert.That(existing.MachinesOf(screw.Id), Is.EqualTo(1));
            Assert.That(existing.Snapshot().Keys, Is.EqualTo(ExpectedSequence.Of(rod.Id, screw.Id)));
            Assert.That(existing.Machines.Select(m => (m.Id, m.Recipe, m.Machines)), Is.EqualTo(ExpectedSequence.Of((rod.Id, (Recipe)rod, 3), (screw.Id, (Recipe)screw, 1))));
        });
    }

    [Test]
    public void Add_KeepsTheLatestRecipeObject()
    {
        var existing = new ExistingCapacity();
        ManufacturingRecipe first = Recipe("Rod");
        ManufacturingRecipe second = first with { Name = "Rod again" };

        existing.Add(first, 1);
        existing.Add(second, 1);

        Assert.That(existing.Machines.Single().Recipe, Is.SameAs(second));
    }

    [Test]
    public void Snapshot_IsACopy()
    {
        var existing = new ExistingCapacity();
        ManufacturingRecipe rod = Recipe("Rod");
        existing.Add(rod, 2);

        IReadOnlyDictionary<RecipeId, int> snapshot = existing.Snapshot();
        existing.Add(rod, 5);
        existing.Add(Recipe("Screw"), 1);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot[rod.Id], Is.EqualTo(2));
            Assert.That(snapshot, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Add_Null_Throws() =>
        Assert.That(() => new ExistingCapacity().Add(null!, 1), Throws.ArgumentNullException);
}
