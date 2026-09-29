using System.Text.Json;

namespace Satisfactory.Planner.Tests.TestVectors;

/// <summary>
/// Loads <c>python_sum_vectors.json</c> (written by <c>generate_python_sum_vectors.py</c> with CPython) from the test
/// output directory: CPython's builtin <c>sum()</c> of lists of floats (and int zeros).
/// </summary>
internal sealed class PythonSumVectors
{
    private const string FileName = "python_sum_vectors.json";

    private static readonly Lazy<PythonSumVectors> LazyInstance = new(Load);

    private PythonSumVectors(string pythonVersion, IReadOnlyList<SumVector> sums)
    {
        PythonVersion = pythonVersion;
        Sums = sums;
    }

    /// <summary>The vectors, loaded once per test run.</summary>
    public static PythonSumVectors Instance => LazyInstance.Value;

    /// <summary>The version of the Python that generated the file.</summary>
    public string PythonVersion { get; }

    /// <summary>One entry per summed list.</summary>
    public IReadOnlyList<SumVector> Sums { get; }

    private static PythonSumVectors Load()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestVectors", FileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        var sums = root.GetProperty("sums").EnumerateArray()
            .Select(e => new SumVector(
                e.GetProperty("values").EnumerateArray().Select(v => v.GetString()!).ToList(),
                e.GetProperty("sum").GetString()!))
            .ToList();
        return new PythonSumVectors(root.GetProperty("python").GetString()!, sums);
    }
}

/// <summary>CPython's <c>sum()</c> of one list.</summary>
/// <param name="Values">Python <c>repr</c> of each item (a float, or the int <c>0</c>).</param>
/// <param name="Sum">Python <c>repr(sum(values))</c>.</param>
internal sealed record SumVector(IReadOnlyList<string> Values, string Sum)
{
    /// <summary>The items as doubles (an int zero becomes <c>0.0</c>).</summary>
    public IReadOnlyList<double> Doubles => Values.Select(PythonNumberVectors.ParseRepr).ToList();

    /// <summary>A short description of the list for failure messages.</summary>
    public string Description => "[" + string.Join(", ", Values.Take(6)) + (Values.Count > 6 ? ", ..." : string.Empty) + "]";
}
