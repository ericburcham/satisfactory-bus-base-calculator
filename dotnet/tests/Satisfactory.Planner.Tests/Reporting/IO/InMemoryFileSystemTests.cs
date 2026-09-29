using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Reporting.IO;

[TestFixture]
public sealed class InMemoryFileSystemTests
{
    [Test]
    public void WriteAllText_StoresContentVerbatimUnderTheNormalizedPath()
    {
        var fileSystem = new InMemoryFileSystem();
        string path = Path.Combine("out", "tier_0_order.csv");

        fileSystem.WriteAllText(path, "a\r\nb\n");

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.ReadAllText(path), Is.EqualTo("a\r\nb\n"));
            Assert.That(fileSystem.ReadAllText("out/./x/../tier_0_order.csv"), Is.EqualTo("a\r\nb\n"));
            Assert.That(fileSystem.FileExists(path), Is.True);
            Assert.That(fileSystem.Files.Keys, Is.EqualTo(new[] { InMemoryFileSystem.Normalize(path) }));
            Assert.That(fileSystem.Files.Keys.Single(), Does.Not.Contain("\\"));
        });
    }

    [Test]
    public void WriteAllText_Twice_KeepsTheLatestContentAndRecordsBothWrites()
    {
        var fileSystem = new InMemoryFileSystem();

        fileSystem.WriteAllText("a.txt", "one");
        fileSystem.WriteAllText("b.txt", "two");
        fileSystem.WriteAllText("a.txt", "three");

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.ReadAllText("a.txt"), Is.EqualTo("three"));
            Assert.That(
                fileSystem.WriteOrder,
                Is.EqualTo(new List<string>
                {
                    InMemoryFileSystem.Normalize("a.txt"),
                    InMemoryFileSystem.Normalize("b.txt"),
                    InMemoryFileSystem.Normalize("a.txt"),
                }));
        });
    }

    [Test]
    public void CreateDirectory_IsTracked()
    {
        var fileSystem = new InMemoryFileSystem();

        fileSystem.CreateDirectory("tiers/");
        fileSystem.CreateDirectory("tiers");

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.DirectoryExists("tiers"), Is.True);
            Assert.That(fileSystem.DirectoryExists("other"), Is.False);
            Assert.That(fileSystem.CreatedDirectories, Has.Count.EqualTo(2));
            Assert.That(fileSystem.CreatedDirectories.Distinct().Count(), Is.EqualTo(1));
            Assert.That(fileSystem.FileExists("tiers"), Is.False);
        });
    }

    [Test]
    public void ReadAllText_OfAnUnwrittenFile_Throws()
    {
        var fileSystem = new InMemoryFileSystem();

        Assert.That(() => fileSystem.ReadAllText("missing.txt"), Throws.InstanceOf<FileNotFoundException>());
    }

    [Test]
    public void AddFile_MakesTheFileExistWithoutRecordingAWrite()
    {
        var fileSystem = new InMemoryFileSystem();

        fileSystem.AddFile("data/satisfactory_data.json", "{}");

        Assert.Multiple(() =>
        {
            Assert.That(fileSystem.FileExists("data/satisfactory_data.json"), Is.True);
            Assert.That(fileSystem.WriteOrder, Is.Empty);
        });
    }
}
