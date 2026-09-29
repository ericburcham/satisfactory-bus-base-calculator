using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Test fake of <see cref="IFileSystem"/> that keeps written files in memory, keyed by normalized path, and records
/// which directories were created.
/// </summary>
/// <remarks>
/// Paths are normalized with <see cref="Path.GetFullPath(string)"/> (relative paths resolve against the current
/// directory), use <c>'/'</c> as separator and have no trailing separator; lookups are ordinal (case-sensitive).
/// Writing a file does not require its directory to have been created.
/// </remarks>
public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);
    private readonly List<string> _writeOrder = [];
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);
    private readonly List<string> _createdDirectories = [];

    /// <summary>The files written so far: normalized path to its latest content.</summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    /// <summary>Normalized paths of every <see cref="WriteAllText"/> call, in call order (repeats included).</summary>
    public IReadOnlyList<string> WriteOrder => _writeOrder;

    /// <summary>Normalized paths passed to <see cref="CreateDirectory"/>, in call order (repeats included).</summary>
    public IReadOnlyList<string> CreatedDirectories => _createdDirectories;

    /// <summary>Normalizes a path the way this file system keys it.</summary>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string full = Path.GetFullPath(path).Replace('\\', '/');
        string root = Path.GetPathRoot(full)?.Replace('\\', '/') ?? string.Empty;
        return full.Length > root.Length ? full.TrimEnd('/') : full;
    }

    /// <summary>Adds a file as if it had been written, without recording a write.</summary>
    public void AddFile(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        _files[Normalize(path)] = content;
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        string normalized = Normalize(path);
        _createdDirectories.Add(normalized);
        _directories.Add(normalized);
    }

    /// <summary>Tells whether <paramref name="path"/> was created with <see cref="CreateDirectory"/>.</summary>
    public bool DirectoryExists(string path) => _directories.Contains(Normalize(path));

    /// <inheritdoc />
    public bool FileExists(string path) => _files.ContainsKey(Normalize(path));

    /// <summary>Returns the content written to <paramref name="path"/>.</summary>
    /// <exception cref="FileNotFoundException">Nothing was written to the path.</exception>
    public string ReadAllText(string path)
    {
        string normalized = Normalize(path);
        return _files.TryGetValue(normalized, out string? content)
            ? content
            : throw new FileNotFoundException($"No file was written to '{normalized}'.", normalized);
    }

    /// <inheritdoc />
    public void WriteAllText(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        string normalized = Normalize(path);
        _files[normalized] = content;
        _writeOrder.Add(normalized);
    }
}
