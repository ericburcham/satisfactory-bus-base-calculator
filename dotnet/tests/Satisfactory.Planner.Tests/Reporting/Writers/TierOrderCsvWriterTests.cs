using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Reporting.Formatting;
using Satisfactory.Planner.Reporting.Writers;
using Satisfactory.Planner.Tests.TestSupport;
using static Satisfactory.Planner.Tests.Reporting.Writers.WriterScenario;

namespace Satisfactory.Planner.Tests.Reporting.Writers;

[TestFixture]
public sealed class TierOrderCsvWriterTests
{
    private const string Header = "step,item,new_machines,building,recipe,alternate,excess_target,sink_disposal,inputs,byproducts,mw";

    private InMemoryFileSystem _fileSystem = null!;
    private TierOrderCsvWriter _writer = null!;

    [SetUp]
    public void SetUp()
    {
        _fileSystem = new InMemoryFileSystem();
        _writer = new TierOrderCsvWriter(_fileSystem, new CsvFormatter(), new PowerCalculator(Data));
    }

    [Test]
    public void Write_RichTier_MatchesPythonOutput_WithCrlfRows()
    {
        _writer.Write(Rich(), RichContext());

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_5_order.csv")),
            Is.EqualTo(Text(
                "\r\n",
                Header,
                "1,Dark Matter,2,Particle Accelerator,Dark Matter From Nothing,,,,,Water; Coal,1000.0",
                "2,Iron Ingot,3,Smelter,Iron Ingot,,,,Iron Ore,,12",
                "3,Packaged Water,1,Packager,Packaged Water,,,\"turns leftover Heavy Oil Residue (1,234.6/min) into something sinkable\",Water; Empty Canister (bus),,10",
                "4,Uranium Waste,2,Nuclear Power Plant,Burn Uranium Fuel Rod,,,,Uranium Fuel Rod (bus); Water,,-5000",
                "5,Iron Plate,2,Constructor,Iron Plate,,yes,,Iron Ingot (#2),,8",
                "6,Reinforced Iron Plate With An Extremely Long Name,1,Assembler Of Very Long Name Mk.1,Alternate: Bolted Iron Plate,yes,yes,,Iron Plate (#5); Screws (bus),,15")));
    }

    [Test]
    public void Write_MinimalTier_MatchesPythonOutput()
    {
        _writer.Write(Minimal(), MinimalContext());

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.csv")),
            Is.EqualTo(Text("\r\n", Header, "1,Iron Ingot,1,Smelter,Iron Ingot,,yes,,Iron Ore,,4")));
    }

    [TestCase(2.25, false, "2.2")]
    [TestCase(2.35, false, "2.4")]
    [TestCase(16.0, false, "16.0")]
    [TestCase(-30000.0, true, "-30000")]
    [TestCase(165.0, true, "165")]
    public void Write_PowerColumn_IsPythonStrOfRoundedPower(double powerMw, bool isInteger, string expected)
    {
        var writer = new TierOrderCsvWriter(_fileSystem, new CsvFormatter(), new FixedPowerCalculator(powerMw, isInteger));

        writer.Write(Minimal(), MinimalContext());

        Assert.That(
            _fileSystem.ReadAllText(Path.Combine(OutputFolder, "tier_0_order.csv")),
            Does.EndWith("\r\n1,Iron Ingot,1,Smelter,Iron Ingot,,yes,,Iron Ore,," + expected + "\r\n"));
    }

    [Test]
    public void Constructor_RejectsNulls()
    {
        var csv = new CsvFormatter();
        var power = new PowerCalculator(Data);
        Assert.Multiple(() =>
        {
            Assert.That(() => new TierOrderCsvWriter(null!, csv, power), Throws.ArgumentNullException);
            Assert.That(() => new TierOrderCsvWriter(_fileSystem, null!, power), Throws.ArgumentNullException);
            Assert.That(() => new TierOrderCsvWriter(_fileSystem, csv, null!), Throws.ArgumentNullException);
        });
    }

    private sealed class FixedPowerCalculator(double powerMw, bool isInteger) : IPowerCalculator
    {
        public double PowerMw(Satisfactory.Planner.Core.Model.Recipe recipe, double machines) => powerMw;

        public bool IsIntegerValued(Satisfactory.Planner.Core.Model.Recipe recipe) => isInteger;
    }
}
