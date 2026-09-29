using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierTotalsCalculatorTests
{
    [Test]
    public void Calculate_RichTier_SeparatesUsedAndGeneratedPower()
    {
        var calculator = new TierTotalsCalculator(new PowerCalculator(Data));

        TierTotals totals = calculator.Calculate(RichContext().BuildOrder);

        // 2 x 500 (variable) + 3 x 4 + 1 x 10 + 2 x 4 + 1 x 15 used; 2 x 2500 generated.
        Assert.That(totals, Is.EqualTo(new TierTotals(11, 1045.0, 5000.0)));
    }

    [Test]
    public void Calculate_EmptyBuildOrder_IsZero()
    {
        TierTotals totals = new TierTotalsCalculator(new PowerCalculator(Data)).Calculate([]);

        Assert.Multiple(() =>
        {
            Assert.That(totals, Is.EqualTo(new TierTotals(0, 0.0, 0.0)));
            Assert.That(double.IsNegative(totals.PowerUsedMw), Is.False);
            Assert.That(double.IsNegative(totals.PowerGeneratedMw), Is.False);
        });
    }

    [Test]
    public void Calculate_AddsInBuildOrder_WithZeroPowerCountedAsUsed()
    {
        // 0.1 + 0.2 + 0.3 in this order is 0.6000000000000001 (a different order gives 0.6).
        var power = new SequencePowerCalculator(0.1, 0.2, 0.3, -0.0, 0.0);
        IReadOnlyList<MachineGroupNode> order = Enumerable.Range(0, 5)
            .Select(i => new MachineGroupNode(RecipeOf("Recipe_IngotIron_C"), i + 1, "p", "b", makesNewItem: false))
            .ToList();

        TierTotals totals = new TierTotalsCalculator(power).Calculate(order);

        Assert.Multiple(() =>
        {
            Assert.That(totals.NewMachines, Is.EqualTo(15));
            Assert.That(totals.PowerUsedMw, Is.EqualTo(0.1 + 0.2 + 0.3 + 0.0 + 0.0));
            Assert.That(totals.PowerGeneratedMw, Is.EqualTo(0.0));
            Assert.That(power.Machines, Is.EqualTo(new double[] { 1, 2, 3, 4, 5 }));
        });
    }

    [Test]
    public void Constructor_RejectsNull()
    {
        Assert.That(() => new TierTotalsCalculator(null!), Throws.ArgumentNullException);
    }

    private sealed class SequencePowerCalculator(params double[] values) : IPowerCalculator
    {
        private int _next;

        public List<double> Machines { get; } = [];

        public double PowerMw(Satisfactory.Planner.Core.Model.Recipe recipe, double machines)
        {
            Machines.Add(machines);
            return values[_next++];
        }

        public bool IsIntegerValued(Satisfactory.Planner.Core.Model.Recipe recipe) => false;
    }
}
