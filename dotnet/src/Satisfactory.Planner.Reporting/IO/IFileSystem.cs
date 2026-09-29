namespace Satisfactory.Planner.Reporting.IO;

/// <summary>The file operations the report writers need.</summary>
public interface IFileSystem
{
    /// <summary>Creates a directory and any missing parents; does nothing when it already exists.</summary>
    /// <param name="path">The directory path.</param>
    void CreateDirectory(string path);

    /// <summary>Tells whether a file exists.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the file exists.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Creates or overwrites a file with <paramref name="content"/> as UTF-8 without a byte order mark, written
    /// verbatim (no newline translation).
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The complete file content.</param>
    void WriteAllText(string path, string content);
}
