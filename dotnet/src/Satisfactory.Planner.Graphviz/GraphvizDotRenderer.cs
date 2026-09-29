using System.ComponentModel;
using System.Globalization;

namespace Satisfactory.Planner.Graphviz;

/// <summary>
/// Renders DOT files with the Graphviz <c>dot</c> executable, running <c>dot -Tsvg &lt;in&gt; -o &lt;out&gt;</c>
/// (Python: <c>subprocess.run(["dot", "-Tsvg", base + ".dot", "-o", base + ".svg"], check=True)</c>).
/// </summary>
public sealed class GraphvizDotRenderer : IDotRenderer
{
    private readonly string _dotExecutable;
    private readonly IProcessRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="GraphvizDotRenderer"/> class.</summary>
    /// <param name="dotExecutable">The path (or name) of the <c>dot</c> executable.</param>
    /// <param name="runner">Runs the executable.</param>
    public GraphvizDotRenderer(string dotExecutable, IProcessRunner runner)
    {
        ArgumentException.ThrowIfNullOrEmpty(dotExecutable);
        ArgumentNullException.ThrowIfNull(runner);
        _dotExecutable = dotExecutable;
        _runner = runner;
    }

    /// <summary>Gets <see langword="true"/>: this renderer always runs Graphviz.</summary>
    public bool IsAvailable => true;

    /// <summary>Runs <c>dot -Tsvg <paramref name="dotPath"/> -o <paramref name="svgPath"/></c>.</summary>
    /// <param name="dotPath">The input DOT file.</param>
    /// <param name="svgPath">The SVG file to write.</param>
    /// <exception cref="GraphvizRenderException">
    /// <c>dot</c> could not be started, or it exited with a non-zero code (the message then includes the exit code
    /// and the standard error text).
    /// </exception>
    public void RenderSvg(string dotPath, string svgPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(dotPath);
        ArgumentException.ThrowIfNullOrEmpty(svgPath);

        string[] arguments = ["-Tsvg", dotPath, "-o", svgPath];
        string command = _dotExecutable + " " + string.Join(' ', arguments);

        ProcessResult result;
        try
        {
            result = _runner.Run(_dotExecutable, arguments);
        }
        catch (Win32Exception e)
        {
            throw new GraphvizRenderException($"Command '{command}' could not be started: {e.Message}", e);
        }

        if (result.ExitCode != 0)
        {
            string message = string.Create(
                CultureInfo.InvariantCulture,
                $"Command '{command}' returned non-zero exit status {result.ExitCode}.");
            string standardError = result.StdErr.Trim();
            if (standardError.Length > 0)
            {
                message += " " + standardError;
            }

            throw new GraphvizRenderException(message, result.ExitCode, result.StdErr);
        }
    }
}
