using Satisfactory.Planner.Cli.CommandLine;
using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Excess;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Core.Power;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;
using Satisfactory.Planner.Core.Solving;
using Satisfactory.Planner.Core.Tiers;
using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Reporting.Formatting;
using Satisfactory.Planner.Reporting.IO;
using Satisfactory.Planner.Reporting.Rendering;
using Satisfactory.Planner.Reporting.Writers;

namespace Satisfactory.Planner.Cli;

/// <summary>
/// Entry point and composition root of the <c>satisfactory-planner</c> console application (Python <c>main</c>):
/// parses the command line, builds the object graph by hand, and runs create output folder, load data, plan every
/// tier, write the reports and print the summary.
/// </summary>
public static class Program
{
    /// <summary>Runs the application on the console.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code (see <see cref="RunAsync"/>).</returns>
    public static Task<int> Main(string[] args) => RunAsync(args, Console.Out, Console.Error);

    /// <summary>Runs the application.</summary>
    /// <param name="args">The command-line arguments, without the program name.</param>
    /// <param name="stdout">Receives the help text, the download notice and the summary.</param>
    /// <param name="stderr">Receives usage errors and failure messages.</param>
    /// <param name="dotRenderer">
    /// Renders the <c>.dot</c> files; <see langword="null"/> uses Graphviz <c>dot</c> from the <c>PATH</c> when it is
    /// installed and renders nothing otherwise.
    /// </param>
    /// <param name="httpHandler">
    /// The HTTP handler for downloading a missing data file (not disposed); <see langword="null"/> uses the default.
    /// </param>
    /// <returns>
    /// 0 on success or after <c>--help</c>; 1 when planning fails (<see cref="PlannerException"/>) or Graphviz fails
    /// (<see cref="GraphvizRenderException"/>); 2 on a usage error (as argparse).
    /// </returns>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter stdout,
        TextWriter stderr,
        IDotRenderer? dotRenderer = null,
        HttpMessageHandler? httpHandler = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);
        string newLine = Environment.NewLine;

        CommandLineParseResult parsed = new CommandLineOptionsParser().Parse(args);
        if (parsed.HelpRequested)
        {
            await stdout.WriteAsync(UsageText.FormatHelp(newLine)).ConfigureAwait(false);
            return 0;
        }

        if (parsed.Options is not { } options)
        {
            await stderr.WriteAsync(UsageText.FormatError(parsed.Error ?? string.Empty, newLine)).ConfigureAwait(false);
            return 2;
        }

        try
        {
            string summary = await RunPlannerAsync(options, stdout, newLine, dotRenderer, httpHandler).ConfigureAwait(false);
            await stdout.WriteAsync(summary).ConfigureAwait(false);
            return 0;
        }
        catch (Exception e) when (e is PlannerException or GraphvizRenderException)
        {
            await stderr.WriteAsync(OneLine(e.Message) + newLine).ConfigureAwait(false);
            return 1;
        }
    }

    /// <summary>
    /// Python <c>main</c> after argument parsing: <c>os.makedirs(out)</c>, load (downloading when missing),
    /// <c>run_tiers</c>, <c>write_tier</c> per tier (txt, csv, dot, svg) and <c>summary.txt</c>.
    /// </summary>
    /// <returns>The summary text, which the caller prints (Python <c>print("\n".join(S))</c>).</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance",
        Justification = "Composition root: the file system and data source are declared as the abstractions the rest of the object graph depends on.")]
    private static async Task<string> RunPlannerAsync(
        PlannerOptions options, TextWriter stdout, string newLine, IDotRenderer? dotRenderer, HttpMessageHandler? httpHandler)
    {
        IFileSystem fileSystem = new PhysicalFileSystem();
        fileSystem.CreateDirectory(options.OutputFolder);

        GameData data;
        using (HttpClient http = httpHandler is null ? new HttpClient() : new HttpClient(httpHandler, disposeHandler: false))
        {
            IGameDataSource source = new DownloadingGameDataSource(
                new JsonGameDataSource(options.DataPath), http, options.DataPath, stdout);
            data = await source.LoadAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // Planning.
        var contexts = new TierContextFactory(data, new UnlockTierResolver(data), options.MinerTier, options.NodePurity);
        var costs = new ResourceCostCalculator(new DefaultResourceWeights());
        var selector = new CheapestRecipeSelector();
        var power = new PowerCalculator(data);
        var optimizer = new ByproductCreditOptimizer(
            costs, selector, new OneMachineExcessPolicy(), new ProductionSolver(power), new FluidDisposalFinderFactory());
        IReadOnlyList<TierResult> results = new TierPlanner(contexts, costs, selector, optimizer).PlanAllTiers(options);

        // Reporting.
        ITierReportWriter[] writers =
        [
            new TierOrderTextWriter(fileSystem, newLine),
            new TierOrderCsvWriter(fileSystem, new CsvFormatter(), power),
        ];
        var renderer = new GraphvizRendererAdapter(
            dotRenderer ?? new DotRendererFactory(new PathExecutableLocator(), new ProcessRunner()).Create());
        var generator = new ReportGenerator(
            data,
            contexts,
            new TierGraphBuilder(),
            new TierBuildOrder(),
            new TierTotalsCalculator(power),
            writers,
            new TierDotWriter(fileSystem, newLine),
            renderer,
            new SummaryWriter(fileSystem, newLine));
        return generator.Generate(results, options.OutputFolder);
    }

    /// <summary>Joins a multi-line message (such as Graphviz's standard error) into one line.</summary>
    private static string OneLine(string message) =>
        string.Join(' ', message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
