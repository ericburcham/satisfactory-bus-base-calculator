using System.ComponentModel;
using Satisfactory.Planner.Graphviz;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class GraphvizDotRendererTests
{
    private static readonly string[] ExpectedArguments = ["-Tsvg", @"out dir\tier_3.dot", "-o", @"out dir\tier_3.svg"];

    [Test]
    public void IsAvailable_IsTrue()
    {
        var renderer = new GraphvizDotRenderer("dot", new FakeProcessRunner());

        Assert.That(renderer.IsAvailable, Is.True);
    }

    [Test]
    public void RenderSvg_RunsDotWithTsvgInputAndOutputArguments()
    {
        var runner = new FakeProcessRunner();
        var renderer = new GraphvizDotRenderer(@"C:\Program Files\Graphviz\bin\dot.exe", runner);

        renderer.RenderSvg(@"out dir\tier_3.dot", @"out dir\tier_3.svg");

        Assert.That(runner.Calls, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(runner.Calls[0].FileName, Is.EqualTo(@"C:\Program Files\Graphviz\bin\dot.exe"));
            Assert.That(runner.Calls[0].Arguments, Is.EqualTo(ExpectedArguments));
        });
    }

    [Test]
    public void RenderSvg_NonZeroExit_ThrowsWithExitCodeAndStandardError()
    {
        var runner = new FakeProcessRunner { Result = new ProcessResult(3, "ignored", "Error: syntax error in line 7\n") };
        var renderer = new GraphvizDotRenderer("dot", runner);

        var exception = Assert.Throws<GraphvizRenderException>(() => renderer.RenderSvg("a.dot", "a.svg"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ExitCode, Is.EqualTo(3));
            Assert.That(exception.StandardError, Is.EqualTo("Error: syntax error in line 7\n"));
            Assert.That(
                exception.Message,
                Is.EqualTo("Command 'dot -Tsvg a.dot -o a.svg' returned non-zero exit status 3. Error: syntax error in line 7"));
        });
    }

    [Test]
    public void RenderSvg_NonZeroExitWithoutStandardError_MessageHasExitCode()
    {
        var runner = new FakeProcessRunner { Result = new ProcessResult(-1, string.Empty, "  \r\n") };
        var renderer = new GraphvizDotRenderer("dot", runner);

        var exception = Assert.Throws<GraphvizRenderException>(() => renderer.RenderSvg("a.dot", "a.svg"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ExitCode, Is.EqualTo(-1));
            Assert.That(exception.Message, Is.EqualTo("Command 'dot -Tsvg a.dot -o a.svg' returned non-zero exit status -1."));
        });
    }

    [Test]
    public void RenderSvg_ZeroExitWithStandardError_DoesNotThrow()
    {
        var runner = new FakeProcessRunner { Result = new ProcessResult(0, string.Empty, "Warning: something minor") };
        var renderer = new GraphvizDotRenderer("dot", runner);

        Assert.That(() => renderer.RenderSvg("a.dot", "a.svg"), Throws.Nothing);
    }

    [Test]
    public void RenderSvg_ProcessCannotStart_ThrowsGraphvizRenderExceptionWrappingTheCause()
    {
        var cause = new Win32Exception(2, "The system cannot find the file specified.");
        var runner = new FakeProcessRunner { ExceptionToThrow = cause };
        var renderer = new GraphvizDotRenderer("dot", runner);

        var exception = Assert.Throws<GraphvizRenderException>(() => renderer.RenderSvg("a.dot", "a.svg"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.InnerException, Is.SameAs(cause));
            Assert.That(exception.ExitCode, Is.Null);
            Assert.That(exception.Message, Does.Contain("dot -Tsvg a.dot -o a.svg"));
        });
    }

    [Test]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new GraphvizDotRenderer(null!, new FakeProcessRunner()), Throws.ArgumentNullException);
            Assert.That(() => new GraphvizDotRenderer(string.Empty, new FakeProcessRunner()), Throws.ArgumentException);
            Assert.That(() => new GraphvizDotRenderer("dot", null!), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void RenderSvg_EmptyPaths_ThrowWithoutRunning()
    {
        var runner = new FakeProcessRunner();
        var renderer = new GraphvizDotRenderer("dot", runner);

        Assert.Multiple(() =>
        {
            Assert.That(() => renderer.RenderSvg(string.Empty, "a.svg"), Throws.ArgumentException);
            Assert.That(() => renderer.RenderSvg("a.dot", string.Empty), Throws.ArgumentException);
            Assert.That(runner.Calls, Is.Empty);
        });
    }
}
