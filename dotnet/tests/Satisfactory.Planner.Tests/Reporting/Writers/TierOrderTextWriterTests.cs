using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierOrderTextWriterTests
{
    /// <summary>The Python <c>write_tier</c> output for <see cref="WriterScenario.Rich"/>.</summary>
    private static readonly string[] RichLines =
    [
        "TIER 5 - NEW CAPACITY (build top to bottom; whole machines at 100% clock)",
        "",
        "New items this tier (one excess machine each):",
        "    Iron Plate                             20.00/min excess  via Iron Plate",
        "    Reinforced Iron Plate With An Extremely Long Name  1234.50/min excess  via Alternate: Bolted Iron Plate  [ALTERNATE]",
        "",
        "Build order:",
        "   1. Dark Matter                       2 x Particle Accelerator   Dark Matter From Nothing",
        "          inputs: (no inputs)",
        "          byproducts: Water, Coal",
        "   2. Iron Ingot                        3 x Smelter                Iron Ingot",
        "          inputs: Iron Ore",
        "   3. Packaged Water                    1 x Packager               Packaged Water",
        "          inputs: Water, Empty Canister (bus)",
        "          -> turns leftover Heavy Oil Residue (1,234.6/min) into something sinkable",
        "   4. Uranium Waste                     2 x Nuclear Power Plant    Burn Uranium Fuel Rod",
        "          inputs: Uranium Fuel Rod (bus), Water",
        "   5. Iron Plate                        2 x Constructor            Iron Plate",
        "          inputs: Iron Ingot (#2)",
        "   6. Reinforced Iron Plate With An Extremely Long Name    1 x Assembler Of Very Long Name Mk.1 Alternate: Bolted Iron Plate  [ALTERNATE]",
        "          inputs: Iron Plate (#5), Screws (bus)",
        "",
        "New machines this tier: 11   power used: 1,045.0 MW at 100% clock   power generated: 5,000 MW (nuclear)",
        "",
        "Spare capacity from earlier tiers now put to work (machines' worth):",
        "    Iron Plate                              0.90",
        "    Iron Ingot                              0.25",
        "    Screws                                  0.25",
        "",
        "To the AWESOME Sink (byproducts not used, plus stored excess is sunk at the far end of the bus):",
        "    Plastic                               1,500.25/min",
        "    Uranium Waste                           100.00/min",
        "    Iron Plate                                7.50/min",
        "    Empty Canister                            7.50/min",
        "",
        "Raw input for the whole bus after this tier:",
        "    Coal                                      0.00/min",
        "    Iron Ore                              1,273.90/min",
        "    Water                                15,328.07/min",
    ];

    /// <summary>The Python <c>write_tier</c> output for <see cref="WriterScenario.Minimal"/>.</summary>
    private static readonly string[] MinimalLines =
    [
        "TIER 0 - ONBOARDING - NEW CAPACITY (build top to bottom; whole machines at 100% clock)",
        "",
        "New items this tier (one excess machine each):",
        "    Iron Ingot                             30.00/min excess  via Iron Ingot",
        "",
        "Build order:",
        "   1. Iron Ingot                        1 x Smelter                Iron Ingot",
        "          inputs: Iron Ore",
        "",
        "New machines this tier: 1   power used: 4.0 MW at 100% clock",
        "",
        "Raw input for the whole bus after this tier:",
    ];

    private InMemoryFileSystem _fileSystem = null!;

    [SetUp]
    public void SetUp() => _fileSystem = new InMemoryFileSystem();

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void Write_RichTier_MatchesPythonOutput(string newLine)
    {
        new TierOrderTextWriter(_fileSystem, newLine).Write(Rich(), RichContext());

        Assert.That(_fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_5_order.txt")), Is.EqualTo(Text(newLine, RichLines)));
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void Write_MinimalTier_OmitsGeneratedPowerReusedAndSink(string newLine)
    {
        new TierOrderTextWriter(_fileSystem, newLine).Write(Minimal(), MinimalContext());

        Assert.That(_fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")), Is.EqualTo(Text(newLine, MinimalLines)));
    }

    [Test]
    public void Write_WritesExactlyOneFile()
    {
        new TierOrderTextWriter(_fileSystem, "\n").Write(Minimal(), MinimalContext());

        Assert.That(_fileSystem.WriteOrder, Is.EqualTo(ExpectedSequence.Of(InMemoryFileSystem.Normalize(Path.Combine(OutputFolder, "tier_0_order.txt")))));
    }

    [Test]
    public void Write_UsesTheContextTotals()
    {
        TierReportContext context = MinimalContext() with { Totals = new TierTotals(7, 1234.56, 0.4) };

        new TierOrderTextWriter(_fileSystem, "\n").Write(Minimal(), context);

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")),
            Does.Contain("\nNew machines this tier: 7   power used: 1,234.6 MW at 100% clock   power generated: 0 MW (nuclear)\n"));
    }

    [Test]
    public void Write_ItemMadeByTwoGroups_ReferencesTheFirstGroupInGraphOrder()
    {
        // Two groups make Iron Ingot; Python maker.setdefault keeps the first one in NewMachines order.
        Recipe ingot = RecipeOf("Recipe_IngotIron_C");
        var otherIngot = (ManufacturingRecipe)ingot with { Id = new RecipeId("Recipe_Alternate_IngotIron_C"), Name = "Alternate: Ingot" };
        Recipe plate = RecipeOf("Recipe_IronPlate_C");
        TierResult result = Minimal() with
        {
            NewMachines = Ordered<RecipeId, int>((ingot.Id, 1), (otherIngot.Id, 1), (plate.Id, 1)),
            Recipes = Ordered<RecipeId, Recipe>((ingot.Id, ingot), (otherIngot.Id, otherIngot), (plate.Id, plate)),
        };
        TierReportContext context = ContextOf(
            result, new TierTotals(3, 12, 0), "Recipe_Alternate_IngotIron_C", "Recipe_IngotIron_C", "Recipe_IronPlate_C");

        new TierOrderTextWriter(_fileSystem, "\n").Write(result, context);

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")),
            Does.Contain("   3. Iron Plate                        1 x Constructor            Iron Plate\n          inputs: Iron Ingot (#2)\n"));
    }

    [Test]
    public void Write_ReusedTies_KeepInsertionOrder()
    {
        TierResult result = Minimal() with
        {
            Reused = Ordered<ItemId, double>((Screw, 0.5), (IronIngot, 0.75), (Coal, 0.5), (IronPlate, 0.5)),
        };

        new TierOrderTextWriter(_fileSystem, "\n").Write(result, MinimalContext());

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")),
            Does.Contain(Text(
                "\n",
                "Spare capacity from earlier tiers now put to work (machines' worth):",
                "    Iron Ingot                              0.75",
                "    Screws                                  0.50",
                "    Coal                                    0.50",
                "    Iron Plate                              0.50",
                "")));
    }

    [Test]
    public void Write_RawInput_IsSortedByNameNotByValueOrInsertion()
    {
        TierResult result = Minimal() with
        {
            RawInput = Ordered<ItemId, double>((Water, 1.0), (OreIron, 3.0), (Coal, 2.0)),
        };

        new TierOrderTextWriter(_fileSystem, "\n").Write(result, MinimalContext());

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")),
            Does.EndWith(Text(
                "\n",
                "Raw input for the whole bus after this tier:",
                "    Coal                                      2.00/min",
                "    Iron Ore                                  3.00/min",
                "    Water                                     1.00/min")));
    }

    [Test]
    public void Write_BusInputWithoutMaker_IsLabelledBus_AndRawInputIsPlain()
    {
        // In the rich graph, Screws come from the bus and Iron Ore is raw.
        new TierOrderTextWriter(_fileSystem, "\n").Write(Rich(), RichContext());

        string text = _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_5_order.txt"));
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("inputs: Iron Plate (#5), Screws (bus)\n"));
            Assert.That(text, Does.Contain("inputs: Iron Ore\n"));
        });
    }

    [Test]
    public void Write_InputNodeTypeDecidesRawOrBus()
    {
        // The graph (built with the final tier's extractors) decides: a bus node for a resource prints "(bus)".
        TierResult result = Minimal();
        var graph = new Satisfactory.Graphs.DirectedAcyclicGraph<TierGraphNode>();
        var machine = new MachineGroupNode(RecipeOf("Recipe_IngotIron_C"), 1, "Iron Ingot", "Smelter", makesNewItem: true);
        graph.AddEdge(new BusInputNode(OreIron, "Iron Ore"), machine);
        machine.Step = 1;
        var context = new TierReportContext(OutputFolder, Data, graph, [machine], new TierTotals(1, 4, 0));

        new TierOrderTextWriter(_fileSystem, "\n").Write(result, context);

        Assert.That(_fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.txt")), Does.Contain("inputs: Iron Ore (bus)\n"));
    }

    [Test]
    public void Constructor_RejectsNulls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new TierOrderTextWriter(null!, "\n"), Throws.ArgumentNullException);
            Assert.That(() => new TierOrderTextWriter(_fileSystem, null!), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void Write_RejectsNulls()
    {
        var writer = new TierOrderTextWriter(_fileSystem, "\n");

        Assert.Multiple(() =>
        {
            Assert.That(() => writer.Write(null!, MinimalContext()), Throws.ArgumentNullException);
            Assert.That(() => writer.Write(Minimal(), null!), Throws.ArgumentNullException);
        });
    }
}
