using Satisfactory.Graphs;

namespace Satisfactory.Planner.Tests.Graphs;

[TestFixture]
public sealed class CycleDetectedExceptionTests
{
    [Test]
    public void EdgeConstructor_NamesTheEdge()
    {
        var exception = new CycleDetectedException("a", "b");

        Assert.Multiple(() =>
        {
            Assert.That(exception.FromId, Is.EqualTo("a"));
            Assert.That(exception.ToId, Is.EqualTo("b"));
            Assert.That(exception.Message, Does.Contain("\"a\" -> \"b\""));
            Assert.That(exception, Is.InstanceOf<InvalidOperationException>());
        });
    }

    [Test]
    public void EdgeConstructor_SelfLoop_MentionsSelfLoop()
    {
        var exception = new CycleDetectedException("a", "a");

        Assert.That(exception.Message, Does.Contain("self-loop").And.Contain("\"a\" -> \"a\""));
    }

    [Test]
    public void EdgeConstructor_NullId_Throws()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new CycleDetectedException(null!, "b"), Throws.ArgumentNullException);
            Assert.That(() => new CycleDetectedException("a", (string)null!), Throws.ArgumentNullException);
        });
    }

    [Test]
    public void StandardConstructors_LeaveIdsEmpty()
    {
        var inner = new InvalidOperationException("inner");
        var withMessage = new CycleDetectedException("message");
        var withInner = new CycleDetectedException("message", inner);
        var parameterless = new CycleDetectedException();

        Assert.Multiple(() =>
        {
            Assert.That(withMessage.Message, Is.EqualTo("message"));
            Assert.That(withMessage.FromId, Is.Empty);
            Assert.That(withMessage.ToId, Is.Empty);
            Assert.That(withInner.InnerException, Is.SameAs(inner));
            Assert.That(withInner.FromId, Is.Empty);
            Assert.That(parameterless.Message, Is.Not.Empty);
            Assert.That(parameterless.ToId, Is.Empty);
        });
    }
}
