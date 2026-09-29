using System.Globalization;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Core.Tiers.Graph;
using Satisfactory.Planner.Reporting.Formatting;
using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Reporting.Writers;

/// <summary>
/// Writes <c>tier_N_order.txt</c>, the human-readable build order of a tier (Python <c>write_tier</c> lines 819-867).
/// </summary>
public sealed class TierOrderTextWriter : ReportWriterBase
{
    private const string AlternateMarker = "  [ALTERNATE]";
    private readonly string _newLine;

    /// <summary>Creates the writer.</summary>
    /// <param name="fileSystem">The file system the report is written to.</param>
    /// <param name="newLine">The line terminator written after every line (the CLI passes <see cref="Environment.NewLine"/>).</param>
    public TierOrderTextWriter(IFileSystem fileSystem, string newLine)
        : base(fileSystem)
    {
        ArgumentNullException.ThrowIfNull(newLine);
        _newLine = newLine;
    }

    /// <inheritdoc />
    protected override string FileName(TierResult result) =>
        string.Create(CultureInfo.InvariantCulture, $"tier_{result.Tier}_order.txt");

    /// <inheritdoc />
    protected override string Render(TierResult result, TierReportContext context)
    {
        GameData data = context.Data;
        var describer = new TierMachineDescriber(result, context);
        string title = TierNames.For(result.Tier);
        var lines = new List<string>
        {
            $"{title.ToUpperInvariant()} - NEW CAPACITY (build top to bottom; whole machines at 100% clock)",
            string.Empty,
            "New items this tier (one excess machine each):",
        };

        foreach (ItemId item in result.NewItems.OrderBy(data.ItemName, StringComparer.Ordinal))
        {
            Recipe recipe = result.ExcessRecipe[item];
            lines.Add(
                $"    {Name(data, item, 35)} {PythonNumberFormat.Fixed(result.Excess[item], 2, 8)}/min excess  via {recipe.Name}"
                + (recipe.IsAlternate ? AlternateMarker : string.Empty));
        }

        lines.Add(string.Empty);
        lines.Add("Build order:");
        foreach (MachineGroupNode machine in context.BuildOrder)
        {
            AddMachine(lines, machine, describer);
        }

        TierTotals totals = context.Totals;
        lines.Add(string.Empty);
        lines.Add(
            string.Create(CultureInfo.InvariantCulture, $"New machines this tier: {totals.NewMachines}")
            + $"   power used: {PythonNumberFormat.FixedThousands(totals.PowerUsedMw, 1)} MW at 100% clock"
            + (totals.PowerGeneratedMw != 0
                ? $"   power generated: {PythonNumberFormat.FixedThousands(totals.PowerGeneratedMw, 0)} MW (nuclear)"
                : string.Empty));

        if (result.Reused.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Spare capacity from earlier tiers now put to work (machines' worth):");
            foreach (KeyValuePair<ItemId, double> reused in result.Reused.OrderBy(kv => -kv.Value))
            {
                lines.Add($"    {Name(data, reused.Key, 35)} {PythonNumberFormat.Fixed(reused.Value, 2, 8)}");
            }
        }

        if (result.Sink.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("To the AWESOME Sink (byproducts not used, plus stored excess is sunk at the far end of the bus):");
            foreach (KeyValuePair<ItemId, double> sink in result.Sink.OrderBy(kv => -kv.Value))
            {
                lines.Add($"    {Name(data, sink.Key, 35)} {PythonNumberFormat.FixedThousands(sink.Value, 2, 10)}/min");
            }
        }

        lines.Add(string.Empty);
        lines.Add("Raw input for the whole bus after this tier:");
        foreach (KeyValuePair<ItemId, double> raw in result.RawInput.OrderBy(kv => data.ItemName(kv.Key), StringComparer.Ordinal))
        {
            lines.Add($"    {Name(data, raw.Key, 35)} {PythonNumberFormat.FixedThousands(raw.Value, 2, 10)}/min");
        }

        return string.Join(_newLine, lines) + _newLine;
    }

    private static string Name(GameData data, ItemId item, int width) => PythonTextFormat.PadRight(data.ItemName(item), width);

    private static void AddMachine(List<string> lines, MachineGroupNode machine, TierMachineDescriber describer)
    {
        Recipe recipe = machine.Recipe;
        string alternate = recipe.IsAlternate ? AlternateMarker : string.Empty;
        lines.Add(
            $"{PythonNumberFormat.Integer(machine.Step, 4)}. {PythonTextFormat.PadRight(machine.ProductName, 30)} "
            + $"{PythonNumberFormat.Integer(machine.NewMachines, 4)} x {PythonTextFormat.PadRight(machine.BuildingName, 22)} "
            + $"{recipe.Name}{alternate}");

        string inputs = string.Join(", ", describer.IngredientSources(recipe));
        lines.Add("          inputs: " + (inputs.Length > 0 ? inputs : "(no inputs)"));

        IReadOnlyList<string> byproducts = describer.ByproductNames(recipe);
        if (byproducts.Count > 0)
        {
            lines.Add("          byproducts: " + string.Join(", ", byproducts));
        }

        string? note = describer.DisposalNote(recipe.Id);
        if (note is not null)
        {
            lines.Add("          -> " + note);
        }
    }
}
