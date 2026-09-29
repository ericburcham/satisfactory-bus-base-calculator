using System.Globalization;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Formatting;
using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Writes <c>tier_N_order.csv</c>, one row per machine group in build order (Python <c>write_tier</c> lines 851-854
/// and 873-875). Rows always end with <c>"\r\n"</c>, as Python's <c>csv.writer</c> writes them.
/// </summary>
public sealed class TierOrderCsvWriter : ReportWriterBase
{
    private static readonly string[] Header =
    [
        "step", "item", "new_machines", "building", "recipe", "alternate", "excess_target", "sink_disposal", "inputs",
        "byproducts", "mw",
    ];

    private readonly ICsvFormatter _csv;
    private readonly IPowerCalculator _power;

    /// <summary>Creates the writer.</summary>
    /// <param name="fileSystem">The file system the report is written to.</param>
    /// <param name="csv">Formats the rows.</param>
    /// <param name="power">Computes the <c>mw</c> column.</param>
    public TierOrderCsvWriter(IFileSystem fileSystem, ICsvFormatter csv, IPowerCalculator power)
        : base(fileSystem)
    {
        ArgumentNullException.ThrowIfNull(csv);
        ArgumentNullException.ThrowIfNull(power);
        _csv = csv;
        _power = power;
    }

    /// <inheritdoc />
    protected override string FileName(TierResult result) =>
        string.Create(CultureInfo.InvariantCulture, $"tier_{result.Tier}_order.csv");

    /// <inheritdoc />
    protected override string Render(TierResult result, TierReportContext context)
    {
        var describer = new TierMachineDescriber(result, context);
        var rows = new List<IReadOnlyList<string>> { Header };
        foreach (MachineGroupNode machine in context.BuildOrder)
        {
            rows.Add(Row(result, machine, describer));
        }

        return _csv.Format(rows);
    }

    private string[] Row(TierResult result, MachineGroupNode machine, TierMachineDescriber describer)
    {
        Recipe recipe = machine.Recipe;
        double powerMw = _power.PowerMw(recipe, machine.NewMachines);
        return
        [
            machine.Step.ToString(CultureInfo.InvariantCulture),
            machine.ProductName,
            machine.NewMachines.ToString(CultureInfo.InvariantCulture),
            machine.BuildingName,
            recipe.Name,
            recipe.IsAlternate ? "yes" : string.Empty,
            result.NewItems.Contains(recipe.MainProduct.Item) ? "yes" : string.Empty,
            describer.DisposalNote(recipe.Id) ?? string.Empty,
            string.Join("; ", describer.IngredientSources(recipe)),
            string.Join("; ", describer.ByproductNames(recipe)),
            PythonNumberFormat.RoundRepr(powerMw, PlannerConstants.PowerRoundingDigits, _power.IsIntegerValued(recipe)),
        ];
    }
}
