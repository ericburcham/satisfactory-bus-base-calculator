using System.Diagnostics;

namespace Satisfactory.Planner.Graphviz;

/// <summary>
/// <see cref="IProcessRunner"/> built on <see cref="Process"/>: no shell, arguments passed through
/// <see cref="ProcessStartInfo.ArgumentList"/>, both output streams captured.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <summary>Starts the process, reads standard output and standard error concurrently, and waits for it to exit.</summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments, each passed verbatim as one argument.</param>
    /// <returns>The exit code and the captured output.</returns>
    /// <exception cref="System.ComponentModel.Win32Exception">The executable could not be started.</exception>
    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Drain both pipes concurrently so a child that fills one pipe's buffer cannot deadlock against us.
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(standardOutput, standardError);

        return new ProcessResult(process.ExitCode, standardOutput.Result, standardError.Result);
    }
}
