using System.ComponentModel;
using System.Runtime.InteropServices;
using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class ProcessRunnerTests
{
    [Test]
    public void Run_DotnetVersion_ExitsZeroWithStandardOutput()
    {
        ProcessResult result = new ProcessRunner().Run(DotnetHost(), ["--version"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StdErr);
            Assert.That(result.StdOut.Trim(), Is.Not.Empty);
        });
    }

    [Test]
    public void Run_DotnetInfo_CapturesMultiLineOutput()
    {
        ProcessResult result = new ProcessRunner().Run(DotnetHost(), ["--info"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StdErr);
            Assert.That(result.StdOut, Does.Contain(".NET"));
            Assert.That(result.StdOut.Split('\n'), Has.Length.GreaterThan(5));
        });
    }

    [Test]
    public void Run_MissingExecutable_ThrowsWin32Exception()
    {
        string missing = Path.Combine(Path.GetTempPath(), "no-such-tool-" + Guid.NewGuid().ToString("N"));

        Assert.That(() => new ProcessRunner().Run(missing, []), Throws.InstanceOf<Win32Exception>());
    }

    [Test]
    public void Run_InvalidArguments_Throw()
    {
        var runner = new ProcessRunner();

        Assert.Multiple(() =>
        {
            Assert.That(() => runner.Run(string.Empty, []), Throws.ArgumentException);
            Assert.That(() => runner.Run("dotnet", null!), Throws.ArgumentNullException);
        });
    }

    // The test host is usually testhost(.exe), not the dotnet muxer, so find the muxer explicitly.
    private static string DotnetHost()
    {
        string? processPath = Environment.ProcessPath;
        if (processPath is not null
            && string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        string? hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(hostPath) && File.Exists(hostPath))
        {
            return hostPath;
        }

        // <dotnet root>/shared/Microsoft.NETCore.App/<version>/
        string dotnetRoot = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));
        string muxer = Path.Combine(dotnetRoot, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        if (File.Exists(muxer))
        {
            return muxer;
        }

        return new PathExecutableLocator().Find("dotnet") ?? "dotnet";
    }
}
