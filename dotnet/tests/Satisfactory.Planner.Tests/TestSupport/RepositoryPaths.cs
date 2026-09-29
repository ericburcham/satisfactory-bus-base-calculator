namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Locates the repository (or worktree) root: the nearest ancestor of the test directory that contains both
/// <c>data/</c> and <c>golden-output/</c>.
/// </summary>
public static class RepositoryPaths
{
    private static readonly Lazy<string> LazyRoot = new(FindRoot);

    /// <summary>The repository root.</summary>
    public static string Root => LazyRoot.Value;

    /// <summary><c>data/satisfactory_data.json</c>.</summary>
    public static string DataFile => Path.Combine(Root, "data", "satisfactory_data.json");

    /// <summary><c>golden-output/</c>.</summary>
    public static string GoldenOutput => Path.Combine(Root, "golden-output");

    /// <summary><c>python/satisfactory_plan_tiers.py</c>.</summary>
    public static string PythonScript => Path.Combine(Root, "python", "satisfactory_plan_tiers.py");

    private static string FindRoot()
    {
        string start = TestContext.CurrentContext.TestDirectory;
        for (DirectoryInfo? dir = new(start); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data"))
                && Directory.Exists(Path.Combine(dir.FullName, "golden-output")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"No ancestor of {start} contains both data/ and golden-output/.");
    }
}
