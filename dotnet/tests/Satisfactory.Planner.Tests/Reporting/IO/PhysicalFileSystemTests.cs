using Satisfactory.Planner.Reporting.IO;

namespace Satisfactory.Planner.Tests.Reporting.IO;

[TestFixture]
public sealed class PhysicalFileSystemTests
{
    private readonly PhysicalFileSystem _fileSystem = new();
    private string _root = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "satisfactory-planner-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public void CreateDirectory_CreatesMissingParents_AndIsIdempotent()
    {
        string nested = Path.Combine(_root, "a", "b");

        _fileSystem.CreateDirectory(nested);
        _fileSystem.CreateDirectory(nested);

        Assert.That(Directory.Exists(nested), Is.True);
    }

    [Test]
    public void WriteAllText_WritesUtf8WithoutBom()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "out.txt");

        _fileSystem.WriteAllText(path, "café");

        Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 0x63, 0x61, 0x66, 0xC3, 0xA9 }));
    }

    [Test]
    public void WriteAllText_WritesNewlinesVerbatim()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "lines.csv");
        const string content = "a,b\r\nc\nd\re\r\n";

        _fileSystem.WriteAllText(path, content);

        Assert.That(File.ReadAllBytes(path), Is.EqualTo("a,b\r\nc\nd\re\r\n"u8.ToArray()));
    }

    [Test]
    public void WriteAllText_OverwritesAnExistingFile()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "out.txt");

        _fileSystem.WriteAllText(path, "a much longer first version");
        _fileSystem.WriteAllText(path, "short");

        Assert.That(File.ReadAllText(path), Is.EqualTo("short"));
    }

    [Test]
    public void WriteAllText_EmptyContent_WritesAnEmptyFile()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "empty.txt");

        _fileSystem.WriteAllText(path, string.Empty);

        Assert.That(File.ReadAllBytes(path), Is.Empty);
    }

    [Test]
    public void FileExists_ReflectsTheDisk()
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, "x.txt");

        bool before = _fileSystem.FileExists(path);
        _fileSystem.WriteAllText(path, "x");

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.False);
            Assert.That(_fileSystem.FileExists(path), Is.True);
            Assert.That(_fileSystem.FileExists(_root), Is.False, "a directory is not a file");
        });
    }

    [Test]
    public void Methods_RejectNullArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => _fileSystem.CreateDirectory(null!), Throws.ArgumentNullException);
            Assert.That(() => _fileSystem.FileExists(null!), Throws.ArgumentNullException);
            Assert.That(() => _fileSystem.WriteAllText(null!, "x"), Throws.ArgumentNullException);
            Assert.That(() => _fileSystem.WriteAllText(Path.Combine(_root, "x"), null!), Throws.ArgumentNullException);
        });
    }
}
