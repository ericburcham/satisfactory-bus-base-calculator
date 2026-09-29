using System.Text;

namespace Satisfactory.Planner.Reporting.IO;

/// <summary>An <see cref="IFileSystem"/> over the real disk.</summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return File.Exists(path);
    }

    /// <inheritdoc />
    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(content);
        File.WriteAllText(path, content, Utf8WithoutBom);
    }
}
