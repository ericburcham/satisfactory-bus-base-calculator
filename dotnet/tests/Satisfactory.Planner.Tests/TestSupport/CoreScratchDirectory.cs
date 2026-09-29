namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// A fresh, uniquely named folder under the NUnit work directory, deleted on dispose. Used by the Core tests for data
/// files written to disk.
/// </summary>
public sealed class CoreScratchDirectory : IDisposable
{
    /// <summary>Creates the folder.</summary>
    public CoreScratchDirectory()
    {
        Path = System.IO.Path.Combine(
            TestContext.CurrentContext.WorkDirectory, "core-scratch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The folder's full path.</summary>
    public string Path { get; }

    /// <summary>A path inside the folder.</summary>
    /// <param name="parts">Relative path parts.</param>
    /// <returns>The combined path.</returns>
    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    /// <summary>Writes a text file inside the folder and returns its path.</summary>
    /// <param name="name">The file name.</param>
    /// <param name="content">The content (UTF-8).</param>
    /// <returns>The file's path.</returns>
    public string WriteFile(string name, string content)
    {
        string path = Combine(name);
        File.WriteAllText(path, content);
        return path;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup.
        }
    }
}
