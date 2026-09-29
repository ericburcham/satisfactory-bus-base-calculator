namespace Satisfactory.Planner.Graphviz;

/// <summary>
/// <see cref="IExecutableLocator"/> that searches the directories of the <c>PATH</c> environment variable in order,
/// modelled on Python's <c>shutil.which</c>. On Windows every <c>PATHEXT</c> extension is tried (the name as given
/// first when it already ends with one of them); elsewhere the name is tried as given. A candidate matches when it
/// is an existing file (not a directory).
/// </summary>
/// <remarks>
/// Unlike <c>shutil.which</c>, the current directory is never searched implicitly (Python does so on Windows and for
/// empty <c>PATH</c> entries), and no execute-permission check is made on non-Windows systems.
/// </remarks>
public sealed class PathExecutableLocator : IExecutableLocator
{
    private const string PathVariable = "PATH";
    private const string PathExtVariable = "PATHEXT";

    // Python's shutil._WIN_DEFAULT_PATHEXT, used when PATHEXT is unset or empty.
    private const string DefaultPathExt = ".COM;.EXE;.BAT;.CMD;.VBS;.JS;.WS;.MSC";

    private readonly Func<string, string?> _environment;
    private readonly bool _isWindows;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathExecutableLocator"/> class that reads the process environment
    /// and applies the lookup rules of the current operating system.
    /// </summary>
    public PathExecutableLocator()
        : this(Environment.GetEnvironmentVariable, OperatingSystem.IsWindows())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PathExecutableLocator"/> class.</summary>
    /// <param name="environment">
    /// Returns the value of an environment variable (<c>PATH</c>, <c>PATHEXT</c>) or <see langword="null"/> when it is
    /// not set.
    /// </param>
    /// <param name="isWindows">
    /// <see langword="true"/> to apply the Windows <c>PATHEXT</c> rules. <c>PATH</c> is always split on
    /// <see cref="Path.PathSeparator"/> of the running system.
    /// </param>
    public PathExecutableLocator(Func<string, string?> environment, bool isWindows)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _environment = environment;
        _isWindows = isWindows;
    }

    /// <summary>Looks up <paramref name="executableName"/> in the <c>PATH</c> directories, in order.</summary>
    /// <param name="executableName">
    /// The command name. When it contains a directory part only that directory is searched.
    /// </param>
    /// <returns>
    /// The path of the first matching file (a <c>PATH</c> entry joined with the file name), or
    /// <see langword="null"/> when nothing matches or <c>PATH</c> is unset or empty.
    /// </returns>
    public string? Find(string executableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        string? directoryPart = Path.GetDirectoryName(executableName);
        string fileName = Path.GetFileName(executableName);
        if (fileName.Length == 0)
        {
            return null;
        }

        IReadOnlyList<string> directories;
        if (!string.IsNullOrEmpty(directoryPart))
        {
            directories = [directoryPart];
        }
        else
        {
            string? path = _environment(PathVariable);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            directories = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        IReadOnlyList<string> candidates = CandidateFileNames(fileName);
        var seen = new HashSet<string>(_isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (string directory in directories)
        {
            if (!seen.Add(directory))
            {
                continue;
            }

            foreach (string candidate in candidates)
            {
                string fullPath = Path.Combine(directory, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private List<string> CandidateFileNames(string fileName)
    {
        if (!_isWindows)
        {
            return [fileName];
        }

        string pathExt = _environment(PathExtVariable) is { Length: > 0 } configured ? configured : DefaultPathExt;
        var extensions = pathExt
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(extension => extension.TrimEnd('.'))
            .Where(extension => extension.Length > 0)
            .ToList();

        var candidates = extensions.Select(extension => fileName + extension).ToList();

        // Like cmd.exe (and shutil.which): the name as given is only tried when it already has an executable extension.
        if (extensions.Exists(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Insert(0, fileName);
        }

        return candidates;
    }
}
