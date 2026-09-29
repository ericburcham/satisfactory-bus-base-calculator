using Satisfactory.Graphs;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierDotWriterTests
{
    private const string Header1 =
        "  rankdir=LR; node [shape=box, style=\"rounded,filled\", fontname=\"Helvetica\", fontsize=10, fillcolor=\"#ffffff\"];";

    private const string Header2 = "  edge [color=\"#888888\", arrowsize=0.6];";

    /// <summary>This writer's output for <see cref="WriterScenario.Rich"/>.</summary>
    private static readonly string[] RichLines =
    [
        "digraph tier {",
        Header1,
        Header2,
        "  labelloc=t; label=\"Tier 5: 11 new machines\"; fontsize=18;",
        "  \"Recipe_DarkMatter_C\" [label=\"#1 Dark Matter\\n2 x Particle Accelerator\\nDark Matter From Nothing\"];",
        "  \"Recipe_IngotIron_C\" [label=\"#2 Iron Ingot\\n3 x Smelter\\nIron Ingot\"];",
        "  \"in_Desc_OreIron_C\" -> \"Recipe_IngotIron_C\";",
        "  \"Recipe_PackagedWater_C\" [label=\"#3 Packaged Water\\n1 x Packager\\nPackaged Water\"];",
        "  \"in_Desc_Water_C\" -> \"Recipe_PackagedWater_C\";",
        "  \"in_Desc_FluidCanister_C\" -> \"Recipe_PackagedWater_C\";",
        "  \"Gen_Nuclear_Uranium\" [label=\"#4 Uranium Waste\\n2 x Nuclear Power Plant\\nBurn Uranium Fuel Rod\"];",
        "  \"in_Desc_NuclearFuelRod_C\" -> \"Gen_Nuclear_Uranium\";",
        "  \"in_Desc_Water_C\" -> \"Gen_Nuclear_Uranium\";",
        "  \"Recipe_IronPlate_C\" [label=\"#5 Iron Plate\\n2 x Constructor\\nIron Plate\", penwidth=2.5, color=\"#2a7a2a\"];",
        "  \"Recipe_IngotIron_C\" -> \"Recipe_IronPlate_C\";",
        "  \"Recipe_Alternate_BoltedPlate_C\" [label=\"#6 Reinforced Iron Plate With An Extremely Long Name\\n1 x Assembler Of Very Long Name Mk.1\\nAlternate: Bolted Iron Plate\", penwidth=2.5, color=\"#2a7a2a\"];",
        "  \"Recipe_IronPlate_C\" -> \"Recipe_Alternate_BoltedPlate_C\";",
        "  \"in_Desc_Screw_C\" -> \"Recipe_Alternate_BoltedPlate_C\";",
        "  \"in_Desc_OreIron_C\" [label=\"Iron Ore\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_Screw_C\" [label=\"Screws\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_NuclearFuelRod_C\" [label=\"Uranium Fuel Rod\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_Water_C\" [label=\"Water\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_FluidCanister_C\" [label=\"Empty Canister\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"SINK\" [label=\"AWESOME Sink\", shape=doubleoctagon, fillcolor=\"#ffd6d6\"];",
        "  \"Recipe_IronPlate_C\" -> \"SINK\" [color=\"#cc3333\", style=dashed, label=\"Iron Plate 8/min\", fontcolor=\"#cc3333\"];",
        "  \"Gen_Nuclear_Uranium\" -> \"SINK\" [color=\"#cc3333\", style=dashed, label=\"Uranium Waste 100/min\", fontcolor=\"#cc3333\"];",
        "  \"in_Desc_FluidCanister_C\" -> \"SINK\" [color=\"#cc3333\", style=dashed, label=\"Empty Canister 8/min\", fontcolor=\"#cc3333\"];",
        "  \"in_Desc_Plastic_C\" [label=\"Plastic\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_Plastic_C\" -> \"SINK\" [color=\"#cc3333\", style=dashed, label=\"Plastic 1,500/min\", fontcolor=\"#cc3333\"];",
        "}",
    ];

    /// <summary>
    /// The Python output for <see cref="WriterScenario.Rich"/>: the same lines, but the input nodes that feed machines
    /// come from a Python set, so their order differs.
    /// </summary>
    private static readonly string[] PythonRichLines =
    [
        .. RichLines[..18],
        "  \"in_Desc_OreIron_C\" [label=\"Iron Ore\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_FluidCanister_C\" [label=\"Empty Canister\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_NuclearFuelRod_C\" [label=\"Uranium Fuel Rod\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_Water_C\" [label=\"Water\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        "  \"in_Desc_Screw_C\" [label=\"Screws\\n(bus)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        .. RichLines[23..],
    ];

    private InMemoryFileSystem _fileSystem = null!;

    [SetUp]
    public void SetUp() => _fileSystem = new InMemoryFileSystem();

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void Write_RichTier_WritesTheLinesInOrder(string newLine)
    {
        string path = new TierDotWriter(_fileSystem, newLine).Write(Rich(), RichContext());

        Assert.Multiple(() =>
        {
            Assert.That(path, Is.EqualTo(Path.Combine(OutputFolder, "tier_5.dot")));
            Assert.That(_fileSystem.ReadAllText(path), Is.EqualTo(Text(newLine, RichLines)));
        });
    }

    [Test]
    public void Write_RichTier_HasTheSameLinesAsThePython()
    {
        string path = new TierDotWriter(_fileSystem, "\n").Write(Rich(), RichContext());

        string[] lines = _fileSystem.ReadAllText(path).Split('\n')[..^1];
        Assert.That(lines, Is.EquivalentTo(PythonRichLines));
    }

    [Test]
    public void Write_MinimalTier_MatchesPythonOutput()
    {
        string path = new TierDotWriter(_fileSystem, "\n").Write(Minimal(), MinimalContext());

        Assert.That(
            _fileSystem.ReadAllText(path),
            Is.EqualTo(Text(
                "\n",
                "digraph tier {",
                Header1,
                Header2,
                "  labelloc=t; label=\"Tier 0 - Onboarding: 1 new machines\"; fontsize=18;",
                "  \"Recipe_IngotIron_C\" [label=\"#1 Iron Ingot\\n1 x Smelter\\nIron Ingot\", penwidth=2.5, color=\"#2a7a2a\"];",
                "  \"in_Desc_OreIron_C\" -> \"Recipe_IngotIron_C\";",
                "  \"in_Desc_OreIron_C\" [label=\"Iron Ore\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
                "}")));
    }

    [Test]
    public void Write_MatchesTheGoldenTierZeroLineFormats()
    {
        // Lines copied from golden-output/tier_0.dot, reproduced with a hand-built graph of the same shape.
        var data = new GameDataBuilder()
            .WithItem("Desc_OreIron_C", "Iron Ore").WithResource("Desc_OreIron_C")
            .WithItem("Desc_IronIngot_C", "Iron Ingot")
            .WithItem("Desc_IronPlate_C", "Iron Plate")
            .WithBuilding("Desc_SmelterMk1_C", "Smelter", 4)
            .WithBuilding("Desc_ConstructorMk1_C", "Constructor", 4)
            .WithRecipe("Recipe_IngotIron_C", "Iron Ingot", "Desc_SmelterMk1_C", 2, [("Desc_OreIron_C", 1)], [("Desc_IronIngot_C", 1)])
            .WithRecipe("Recipe_IronPlate_C", "Iron Plate", "Desc_ConstructorMk1_C", 6, [("Desc_IronIngot_C", 3)], [("Desc_IronPlate_C", 2)])
            .Build();
        var ingot = new MachineGroupNode(data.Recipes[0], 2, "Iron Ingot", "Smelter", makesNewItem: false) { Step = 3 };
        var plate = new MachineGroupNode(data.Recipes[1], 1, "Iron Plate", "Constructor", makesNewItem: true) { Step = 4 };
        var ore = new RawResourceInputNode(WriterScenario.OreIron, "Iron Ore");
        var graph = new DirectedAcyclicGraph<TierGraphNode>();
        graph.AddNode(ingot);
        graph.AddNode(plate);
        graph.AddEdge(ore, ingot);
        graph.AddEdge(ingot, plate);
        var context = new TierReportContext(OutputFolder, data, graph, [ingot, plate], new TierTotals(13, 52, 0));

        string path = new TierDotWriter(_fileSystem, "\n").Write(Minimal(), context);

        string[] golden = File.ReadAllLines(Path.Combine(RepositoryPaths.GoldenOutput, "tier_0.dot"));
        string[] expected =
        [
            "  labelloc=t; label=\"Tier 0 - Onboarding: 13 new machines\"; fontsize=18;",
            "  \"Recipe_IngotIron_C\" [label=\"#3 Iron Ingot\\n2 x Smelter\\nIron Ingot\"];",
            "  \"in_Desc_OreIron_C\" -> \"Recipe_IngotIron_C\";",
            "  \"Recipe_IronPlate_C\" [label=\"#4 Iron Plate\\n1 x Constructor\\nIron Plate\", penwidth=2.5, color=\"#2a7a2a\"];",
            "  \"Recipe_IngotIron_C\" -> \"Recipe_IronPlate_C\";",
            "  \"in_Desc_OreIron_C\" [label=\"Iron Ore\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
        ];
        string[] written = _fileSystem.ReadAllText(path).Split('\n')[..^1];
        Assert.Multiple(() =>
        {
            Assert.That(golden, Is.SupersetOf(expected));
            Assert.That(written, Is.EqualTo(new[] { golden[0], golden[1], golden[2] }.Concat(expected).Append(golden[^1])));
        });
    }

    [Test]
    public void Write_InputFeedingBothAMachineAndTheSink_IsWrittenOnceBeforeTheSink()
    {
        string path = new TierDotWriter(_fileSystem, "\n").Write(Rich(), RichContext());

        string[] lines = _fileSystem.ReadAllText(path).Split('\n');
        int node = Array.FindIndex(lines, l => l.StartsWith("  \"in_Desc_FluidCanister_C\" [", StringComparison.Ordinal));
        int sink = Array.IndexOf(lines, "  \"SINK\" [label=\"AWESOME Sink\", shape=doubleoctagon, fillcolor=\"#ffd6d6\"];");
        Assert.Multiple(() =>
        {
            Assert.That(lines.Count(l => l.StartsWith("  \"in_Desc_FluidCanister_C\" [", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(node, Is.LessThan(sink));
        });
    }

    [Test]
    public void Render_CalledDirectly_OmitsTheTitleAndOrdersMachinesByStep()
    {
        var writer = new TierDotWriter(_fileSystem, "\n");
        TierReportContext context = MinimalContext();

        string text = writer.Render(context.Graph, "tier");

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo(Text(
                "\n",
                "digraph tier {",
                Header1,
                Header2,
                "  \"Recipe_IngotIron_C\" [label=\"#1 Iron Ingot\\n1 x Smelter\\nIron Ingot\", penwidth=2.5, color=\"#2a7a2a\"];",
                "  \"in_Desc_OreIron_C\" -> \"Recipe_IngotIron_C\";",
                "  \"in_Desc_OreIron_C\" [label=\"Iron Ore\\n(raw)\", shape=ellipse, fillcolor=\"#eeeeee\"];",
                "}")));
            Assert.That(_fileSystem.WriteOrder, Is.Empty);
        });
    }

    [Test]
    public void Write_RejectsNulls()
    {
        var writer = new TierDotWriter(_fileSystem, "\n");

        Assert.Multiple(() =>
        {
            Assert.That(() => new TierDotWriter(null!, "\n"), Throws.ArgumentNullException);
            Assert.That(() => new TierDotWriter(_fileSystem, null!), Throws.ArgumentNullException);
            Assert.That(() => writer.Write(null!, MinimalContext()), Throws.ArgumentNullException);
            Assert.That(() => writer.Write(Minimal(), null!), Throws.ArgumentNullException);
        });
    }
}
