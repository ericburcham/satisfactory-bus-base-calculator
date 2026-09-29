# Satisfactory per-tier planner (.NET 10)

A .NET 10 port of `python/satisfactory_plan_tiers.py`. It walks the game's tiers in order; in each tier every item
that first becomes automatable gets one full excess machine, using the unlocked recipe (vanilla or alternate) that
needs the fewest natural resources. Machines built in earlier tiers stay built and their spare capacity is used before
new machines are built. For each tier it writes the new machines in build order (text, CSV and a Graphviz graph), plus
a summary of all tiers.

The port reproduces the Python output exactly: `golden-output/` (made by the Python script) is checked by an automated
test, and the planner's intermediate results are checked bit for bit against dumps of the Python's `run_tiers`.

## Prerequisites

- .NET SDK 10.
- Optional: Graphviz, with `dot` on the `PATH`, to render each `tier_N.dot` to `tier_N.svg`. Without it, the `.svg`
  files are simply not written (as in the Python script). Nothing else, including the tests, needs Graphviz.

All commands below run from the repository root (the folder that contains `data/`, `golden-output/` and `dotnet/`).

## Build

```
dotnet build dotnet/SatisfactoryPlanner.slnx
```

The build treats warnings as errors and requires XML documentation on every public type and member.

## Test

```
dotnet test dotnet/SatisfactoryPlanner.slnx                                    # everything
dotnet test dotnet/SatisfactoryPlanner.slnx --filter "TestCategory!=Golden"    # everything except the golden test
dotnet test dotnet/SatisfactoryPlanner.slnx --filter "TestCategory=Golden"     # only the golden test
dotnet test dotnet/SatisfactoryPlanner.slnx --filter "TestCategory=Parity"     # only the planner parity test
```

- **Golden** (`tests/Satisfactory.Planner.Tests/Golden/GoldenOutputTests.cs`): runs the whole command line once with the
  options that produced `golden-output/` (the defaults) into a temporary folder, then checks that the produced
  non-`.svg` files are exactly the golden ones, compares each file (one test case per golden file), checks that the
  console output is the summary, and checks the line endings the program writes. A mismatch reports the file, the
  1-based line number and both versions of the first differing line (for `.dot` files: the missing and extra lines).
- **Parity** (`tests/Satisfactory.Planner.Tests/Core/Tiers/TierPlannerParityTests.cs`, with the Python dumps and
  `dump_python_tiers.py` in `TestVectors/PlannerParity/`): runs the .NET planner on
  `data/satisfactory_data.json` and compares every tier result and every optimisation round with a dump of the Python
  `run_tiers` (two configurations: the defaults, and `--miner mk2 --purity pure --include-ingots`). Floats are
  compared as Python `repr` strings, so they must be bit-identical; dictionary contents must be in the same order.

The tests need neither Graphviz nor the network.

## Run

```
dotnet run --project dotnet/src/Satisfactory.Planner.Cli -- --data data/satisfactory_data.json --out tiers
```

The built executable is `satisfactory-planner` (`satisfactory-planner.exe` on Windows), in
`dotnet/src/Satisfactory.Planner.Cli/bin/<Configuration>/net10.0/`:

```
satisfactory-planner --data data/satisfactory_data.json --out tiers --miner mk2 --purity pure
```

### Options

The command line behaves like the Python script's `argparse` parser: `--option value` and `--option=value`, unique
prefixes of long options (`--inc` for `--include-ingots`), and the same help and error texts (with the program name
`satisfactory-planner`).

| Option | Default | Meaning |
| --- | --- | --- |
| `--data DATA` | `data/satisfactory_data.json` | The game data file (greeny/SatisfactoryTools `data.json`). If it does not exist, it is downloaded there from `https://raw.githubusercontent.com/greeny/SatisfactoryTools/dev/data/data.json` (creating its folder), after printing `<path> not found; downloading <url>`. |
| `--out OUT` | `tiers` | The folder the output files are written to; created if missing. |
| `--miner {mk1,mk2,mk3}` | `mk1` | The miner used for solid resources (60, 120 or 240 per minute on a normal node). |
| `--purity {impure,normal,pure}` | `normal` | The node purity for solid resources (x0.5, x1, x2). |
| `--include-ingots` | off | Also give ingots their own excess machine. |
| `-h`, `--help` | | Print the help and exit. |

The `--data` default differs from the Python's (`satisfactory_data.json` in the current folder) by design: the data
file of this repository lives in `data/`.

### Output files

Per tier N from 0 to 9, in the `--out` folder:

- `tier_N_order.txt`: the tier's new capacity for people: the new items with their excess rate and recipe; the new
  machine groups in build order (step, product, machine count, building, recipe, `[ALTERNATE]` marker), each with its
  inputs (the step that makes them, `(bus)` for items from earlier tiers, or the plain name for raw resources), its
  byproducts and, for machines that turn a leftover fluid into something sinkable, a note; the new machines and power
  used (and nuclear power generated) this tier; spare capacity of earlier tiers now put to work; what goes to the
  AWESOME Sink; and the raw input of the whole bus after this tier.
- `tier_N_order.csv`: one row per new machine group in build order, with the columns `step, item, new_machines,
  building, recipe, alternate, excess_target, sink_disposal, inputs, byproducts, mw`.
- `tier_N.dot`: a Graphviz graph of the tier's new machine groups, their inputs from the bus and raw resources, and the
  AWESOME Sink.
- `tier_N.svg`: `tier_N.dot` rendered by Graphviz, only when `dot` is on the `PATH`.

And once:

- `summary.txt`: one line per tier with the new items, new machines, alternate recipes, reused spare capacity, power
  used and the change in raw input, followed by the running totals of machines, power used, nuclear power generated
  and raw input (water excluded). The same text is printed to the console at the end.

### Line endings

- Text files (`.txt`, `.dot`, `summary.txt`) and the console use the platform newline (`\r\n` on Windows, `\n`
  elsewhere), as Python's text mode does.
- CSV files always use `\r\n`, as Python's `csv.writer` does.
- The golden test normalises `\r\n` to `\n` on both sides before comparing, because the line endings of the checked-out
  `golden-output/` depend on git's `core.autocrlf`, not on the program. A separate test pins the program's own line
  endings. `.dot` files are compared as multisets of lines, because the Python writes the `in_*` input nodes in `set`
  order.

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Success, or `--help`. |
| 1 | The planner failed (`PlannerException`: bad or missing data, a failed or timed-out download, an empty `--data` or `--out`, or one of the Python's `sys.exit` conditions), Graphviz failed (`GraphvizRenderException`), or a file-system operation failed (`IOException`, `UnauthorizedAccessException`: the output folder or a report cannot be created or written, the data file cannot be read). The message is written to standard error as one line. Python exits with 1 on every uncaught error too. |
| 2 | Usage error: the usage and `satisfactory-planner: error: <message>` are written to standard error. |

## Project map

```
Satisfactory.Graphs              (no references)
Satisfactory.Planner.Core        -> Graphs
Satisfactory.Planner.Reporting   -> Core, Graphs
Satisfactory.Planner.Graphviz    (no references)
Satisfactory.Planner.Cli         -> Core, Reporting, Graphviz
Satisfactory.Planner.Tests       -> all five
```

| Project | Contents |
| --- | --- |
| `src/Satisfactory.Graphs` | A generic directed acyclic graph (`DirectedAcyclicGraph<TNode>`, `GraphEdge<TNode>`, `IGraphNode`, `CycleDetectedException`): insertion-ordered nodes and edges, cycle rejection, longest-path depths, topological sort, subgraphs. Nothing Satisfactory-specific. |
| `src/Satisfactory.Planner.Core` | The planner. `Identifiers` (strongly typed ids), `Model` (items, buildings, recipes, schematics, `GameData`), `Data` (JSON loading and download), `Configuration` (options and constants), `Extraction` (extractor rates), `Progression` (unlock tiers, per-tier context), `Costing` (resource cost per item), `Selection` (recipe choice), `Excess` (excess rate per new item), `Power`, `Solving` (production solver and byproduct optimisation), `Tiers` (the tier walk, the tier graph and its build order) and `Numerics` (`PythonNumerics`: Python's `round`, float formatting, `repr`, `sum`, `max`/`min`). |
| `src/Satisfactory.Planner.Reporting` | The output files: `Formatting` (Python number and text formatting, CSV), `IO` (`IFileSystem`), `Writers` (text, CSV, DOT, summary, per-tier totals) and `Rendering` (`ReportGenerator`, which writes all tiers and the summary, and `IGraphRenderer`). |
| `src/Satisfactory.Planner.Graphviz` | Running Graphviz: locating `dot` on the `PATH`, running it, and a renderer that does nothing when it is missing. |
| `src/Satisfactory.Planner.Cli` | The `satisfactory-planner` executable: `Program` (the composition root, which builds the object graph by hand), the `argparse`-compatible command-line parser and usage text, and `GraphvizRendererAdapter`. |
| `tests/Satisfactory.Planner.Tests` | NUnit tests for every public class, the parity test and the golden test, in folders that mirror the libraries. `TestVectors/` holds only the Python-generated reference data, the scripts that produce it and the classes that load it. |

Patterns used:

- **Strategy**: `IRecipeSelector` (`CheapestRecipeSelector`) and `IExcessPolicy` (`OneMachineExcessPolicy`).
- **Decorator**: `DownloadingGameDataSource` wraps another `IGameDataSource` and downloads a missing file first.
- **Null Object**: `NoExistingCapacity` (no machines built yet) and `NullDotRenderer` (no Graphviz).
- **Template Method**: `ReportWriterBase` (file name and content hooks) and the hooks of `GraphvizDotWriter<TNode>`.
- **Builder**: `TierGraphBuilder` builds a tier's graph from its planning result.
- **Factory**: `TierContextFactory`, `DotRendererFactory` and `FluidDisposalFinderFactory`.
- **Adapter**: `GraphvizRendererAdapter` adapts a Graphviz `IDotRenderer` to the reporting layer's `IGraphRenderer`.

Every dependency is an interface passed to a constructor; there is no DI container and no static mutable state.

## Python test vectors and parity dumps

The tests compare against data produced by the Python reference (CPython 3.14). The scripts live next to their JSON
files, use fixed seeds or deterministic runs, and reproduce the committed files exactly. The scripts that run the
planner need `python/satisfactory_plan_tiers.py` (the Python reference, not tracked by git) and
`data/satisfactory_data.json`. Run them from the repository root:

```
# Float formatting, round() and repr() (python_number_vectors.json)
python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/generate_python_vectors.py

# Builtin sum() of floats, compensated (python_sum_vectors.json)
python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/generate_python_sum_vectors.py

# Per-tier planner decisions for the tier graph and build order tests (tier_graph_vectors.json)
python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/TierGraph/generate_tier_graph_vectors.py

# run_tiers dumps for the parity test
python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/PlannerParity/dump_python_tiers.py
python dotnet/tests/Satisfactory.Planner.Tests/TestVectors/PlannerParity/dump_python_tiers.py --miner mk2 --purity pure --include-ingots --out python_tier_results_mk2_pure_ingots.json
```

`golden-output/` itself is the output of the Python script with its default options (`--miner mk1 --purity normal`,
no `--include-ingots`) on `data/satisfactory_data.json`.

## Parity notes

What the port does to produce the same output as the Python:

- **Dictionary order.** Python dicts keep insertion order and the results depend on it. Every such dict is an
  `OrderedDictionary<TKey, TValue>`, with Python's access semantics (`d[k] += x` on a `defaultdict` appends a missing
  key; `d.get(k, 0)` does not). Sets are only iterated after sorting (ordinally); sorts are stable.
- **Floating point.** Every expression keeps the Python's operation order (`amount * 60 / time` is not
  `amount * (60 / time)`). Python's two-argument `max`/`min`, `math.ceil` and the infinity checks are reproduced.
- **Rounding and formatting.** `round(x, n)` and `f"{x:.2f}"`-style formatting round half-to-even on the exact binary
  value (`BigInteger` arithmetic), unlike `Math.Round`; `repr`/`str` of floats uses the shortest round-trip digits and
  Python's layout (`16.0`, `1e-05`). Padding, thousands separators and `-0.00` follow Python's format mini-language.
- **`sum()`.** Since Python 3.12, `sum()` of floats uses Neumaier's compensated summation, not left-to-right
  addition; `PythonNumerics.Sum` ports CPython's algorithm and is used wherever the Python calls `sum()` (resource
  costs, credits, disposal cost, raw input and reused capacity in the summary).
- **The `mw` CSV column** is `str(round(w, 1))`, and `w` is a Python int for generators and for buildings whose JSON
  `powerConsumption` is an integer literal (printed as `4`, `-30000`), but a float for variable-power recipes
  (`1000.0`). The port keeps track of which one the Python would have.
- **The final tier's extractors.** The Python writes every tier's files after planning the last tier, so every
  report sees the last tier's extractor lock state (raw versus bus inputs); the port does the same.
- **Text encoding.** The port writes UTF-8 without a BOM; Python on Windows writes the locale encoding. The difference
  cannot show with the current data: no name that can appear in the reports contains a non-ASCII character.
- **DOT quoting.** The Python writes names into `.dot` labels and node ids unescaped, so a name containing `"` or a
  newline would give invalid DOT and the Python run would fail when Graphviz renders it. The port escapes `"` as `\"`
  and a newline as `\n`. This cannot show with the current data: no item, building or recipe name contains either
  character.
- **Error exit status.** The Python ends every uncaught error (an unwritable output folder, an unreadable data file, a
  failed download) with a traceback and exit status 1. The port prints the error's message as one line and also exits
  with 1. Like Python's `os.path.exists`, an existing directory given as `--data` counts as present, so it is not
  downloaded over; reading it then fails.
