using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class GraphvizRenderExceptionTests
{
    [Test]
    public void StandardConstructors_SetMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");

        var withMessage = new GraphvizRenderException("failed");
        var withInner = new GraphvizRenderException("failed", inner);

        Assert.Multiple(() =>
        {
            Assert.That(new GraphvizRenderException().ExitCode, Is.Null);
            Assert.That(withMessage.Message, Is.EqualTo("failed"));
            Assert.That(withMessage.StandardError, Is.Null);
            Assert.That(withInner.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void ProcessConstructor_CarriesExitCodeAndStandardError()
    {
        var exception = new GraphvizRenderException("dot failed with 2", 2, "bad input");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("dot failed with 2"));
            Assert.That(exception.ExitCode, Is.EqualTo(2));
            Assert.That(exception.StandardError, Is.EqualTo("bad input"));
        });
    }
}
