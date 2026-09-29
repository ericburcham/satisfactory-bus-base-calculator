using System.Globalization;
using System.Text.Json;

namespace Satisfactory.Planner.Tests.TestVectors;

/// <summary>
/// Loads <c>python_number_vectors.json</c> (written by <c>generate_python_vectors.py</c> with CPython) from the test
/// output directory: CPython's own output for float format specs, <c>round(x, n)</c>, <c>repr(x)</c> and int specs.
/// </summary>
internal sealed class PythonNumberVectors
{
    private const string FileName = "python_number_vectors.json";

    private static readonly Lazy<PythonNumberVectors> LazyInstance = new(Load);

    private PythonNumberVectors(
        string pythonVersion,
        IReadOnlyList<string> formatSpecs,
        IReadOnlyList<int> roundDigits,
        IReadOnlyList<string> intSpecs,
        IReadOnlyList<FloatVector> floats,
        IReadOnlyList<IntVector> ints)
    {
        PythonVersion = pythonVersion;
        FormatSpecs = formatSpecs;
        RoundDigits = roundDigits;
        IntSpecs = intSpecs;
        Floats = floats;
        Ints = ints;
    }

    /// <summary>The vectors, loaded once per test run.</summary>
    public static PythonNumberVectors Instance => LazyInstance.Value;

    /// <summary>The version of the Python that generated the file.</summary>
    public string PythonVersion { get; }

    /// <summary>Float format specs such as <c>".2f"</c> or <c>"10,.2f"</c>, in <see cref="FloatVector.Formatted"/> order.</summary>
    public IReadOnlyList<string> FormatSpecs { get; }

    /// <summary>The <c>ndigits</c> values, in <see cref="FloatVector.RoundedRepr"/> order.</summary>
    public IReadOnlyList<int> RoundDigits { get; }

    /// <summary>Int format specs such as <c>"4d"</c>, in <see cref="IntVector.Formatted"/> order.</summary>
    public IReadOnlyList<string> IntSpecs { get; }

    /// <summary>One entry per double.</summary>
    public IReadOnlyList<FloatVector> Floats { get; }

    /// <summary>One entry per integer.</summary>
    public IReadOnlyList<IntVector> Ints { get; }

    /// <summary>Parses a Python <c>repr</c> of a float (including <c>inf</c>, <c>-inf</c> and <c>nan</c>).</summary>
    public static double ParseRepr(string repr) => repr switch
    {
        "inf" => double.PositiveInfinity,
        "-inf" => double.NegativeInfinity,
        "nan" => double.NaN,
        _ => double.Parse(repr, NumberStyles.Float, CultureInfo.InvariantCulture),
    };

    /// <summary>Parses a float format spec <c>[width][,].{decimals}f</c>.</summary>
    public static (int Width, bool Thousands, int Decimals) ParseFloatSpec(string spec)
    {
        if (!spec.EndsWith('f'))
        {
            throw new FormatException($"Unsupported float spec '{spec}'.");
        }

        int dot = spec.IndexOf('.', StringComparison.Ordinal);
        string head = spec[..dot];
        bool thousands = head.EndsWith(',');
        string width = thousands ? head[..^1] : head;
        return (
            width.Length == 0 ? 0 : int.Parse(width, CultureInfo.InvariantCulture),
            thousands,
            int.Parse(spec[(dot + 1)..^1], CultureInfo.InvariantCulture));
    }

    /// <summary>Parses an int format spec <c>[width]d</c>.</summary>
    public static int ParseIntSpec(string spec) =>
        spec.Length == 1 ? 0 : int.Parse(spec[..^1], CultureInfo.InvariantCulture);

    /// <summary>
    /// Asserts that every actual output equals the expected one (ordinal), listing the first mismatches on failure.
    /// </summary>
    public static void AssertAllMatch(string description, IEnumerable<(string Input, string Expected, string Actual)> results)
    {
        var all = results.ToList();
        var mismatches = all.Where(r => !string.Equals(r.Expected, r.Actual, StringComparison.Ordinal)).ToList();
        Assert.That(all, Is.Not.Empty, $"{description}: no vectors");
        Assert.That(
            mismatches,
            Is.Empty,
            () => $"{description}: {mismatches.Count} of {all.Count} vectors differ from Python; first: "
                + string.Join("; ", mismatches.Take(15).Select(m => $"x={m.Input} expected '{m.Expected}' got '{m.Actual}'")));
    }

    private static PythonNumberVectors Load()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestVectors", FileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        var floats = root.GetProperty("floats").EnumerateArray()
            .Select(e => new FloatVector(
                e.GetProperty("x").GetString()!,
                Strings(e.GetProperty("format")),
                Strings(e.GetProperty("round"))))
            .ToList();
        var ints = root.GetProperty("ints").EnumerateArray()
            .Select(e => new IntVector(e.GetProperty("n").GetInt64(), Strings(e.GetProperty("format"))))
            .ToList();
        return new PythonNumberVectors(
            root.GetProperty("python").GetString()!,
            Strings(root.GetProperty("formatSpecs")),
            root.GetProperty("roundDigits").EnumerateArray().Select(e => e.GetInt32()).ToList(),
            Strings(root.GetProperty("intSpecs")),
            floats,
            ints);
    }

    private static List<string> Strings(JsonElement array) =>
        array.EnumerateArray().Select(e => e.GetString()!).ToList();
}

/// <summary>CPython's output for one double.</summary>
/// <param name="Repr">Python <c>repr(x)</c>; it round-trips, so it also identifies the input.</param>
/// <param name="Formatted">Output of each <see cref="PythonNumberVectors.FormatSpecs"/> spec.</param>
/// <param name="RoundedRepr"><c>repr(round(x, n))</c> for each <see cref="PythonNumberVectors.RoundDigits"/> value.</param>
internal sealed record FloatVector(string Repr, IReadOnlyList<string> Formatted, IReadOnlyList<string> RoundedRepr)
{
    /// <summary>The input double.</summary>
    public double Value => PythonNumberVectors.ParseRepr(Repr);
}

/// <summary>CPython's output for one integer.</summary>
/// <param name="Value">The integer.</param>
/// <param name="Formatted">Output of each <see cref="PythonNumberVectors.IntSpecs"/> spec.</param>
internal sealed record IntVector(long Value, IReadOnlyList<string> Formatted);
