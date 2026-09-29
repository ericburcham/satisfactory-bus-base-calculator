using Satisfactory.Planner.Graphviz;

namespace Satisfactory.Planner.Tests.Graphviz;

[TestFixture]
public sealed class PathExecutableLocatorTests
{
    private static readonly string[] PathAndPathExt = ["PATH", "PATHEXT"];

    private string _root = string.Empty;
    private string _first = string.Empty;
    private string _second = string.Empty;
    private string _missing = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "path-locator-tests-" + Guid.NewGuid().ToString("N"));
        _first = Directory.CreateDirectory(Path.Combine(_root, "first")).FullName;
        _second = Directory.CreateDirectory(Path.Combine(_root, "second")).FullName;
        _missing = Path.Combine(_root, "missing");
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
    public void Find_Windows_AppendsPathExtExtension()
    {
        string tool = CreateFile(_second, "fake-tool.exe");
        var locator = Windows(PathOf(_missing, _first, _second), ".com;.exe;.bat");

        Assert.That(locator.Find("fake-tool"), Is.EqualTo(tool));
    }

    [Test]
    public void Find_Windows_TriesExtensionsInPathExtOrder()
    {
        string bat = CreateFile(_first, "fake-tool.bat");
        CreateFile(_first, "fake-tool.exe");
        var locator = Windows(PathOf(_first), ".bat;.exe");

        Assert.That(locator.Find("fake-tool"), Is.EqualTo(bat));
    }

    [Test]
    public void Find_Windows_NameWithExecutableExtensionIsTriedAsGiven()
    {
        string tool = CreateFile(_first, "fake-tool.exe");
        var locator = Windows(PathOf(_first), ".com;.exe");

        Assert.That(locator.Find("fake-tool.exe"), Is.EqualTo(tool));
    }

    [Test]
    public void Find_Windows_PathExtUnset_UsesDefaultExtensionsIncludingExe()
    {
        string tool = CreateFile(_first, "fake-tool.EXE");
        var locator = Windows(PathOf(_first), pathExt: null);

        Assert.That(locator.Find("fake-tool"), Is.EqualTo(tool));
    }

    [Test]
    public void Find_Windows_FileWithoutExecutableExtensionIsNotAMatch()
    {
        CreateFile(_first, "fake-tool");
        var locator = Windows(PathOf(_first), ".com;.exe");

        Assert.That(locator.Find("fake-tool"), Is.Null);
    }

    [Test]
    public void Find_Windows_MissingExecutable_ReturnsNull()
    {
        var locator = Windows(PathOf(_first, _second), ".exe");

        Assert.That(locator.Find("fake-tool"), Is.Null);
    }

    [Test]
    public void Find_NonWindows_FindsFileByExactName()
    {
        string tool = CreateFile(_second, "fake-tool");
        var locator = NonWindows(PathOf(_missing, _first, _second));

        Assert.That(locator.Find("fake-tool"), Is.EqualTo(tool));
    }

    [Test]
    public void Find_NonWindows_DoesNotAppendExtensions()
    {
        CreateFile(_first, "fake-tool.exe");
        var locator = NonWindows(PathOf(_first));

        Assert.That(locator.Find("fake-tool"), Is.Null);
    }

    [Test]
    public void Find_ReturnsMatchFromFirstPathEntryThatHasIt()
    {
        string first = CreateFile(_first, "fake-tool");
        CreateFile(_second, "fake-tool");
        var locator = NonWindows(PathOf(_first, _second));

        Assert.That(locator.Find("fake-tool"), Is.EqualTo(first));
    }

    [Test]
    public void Find_DirectoryWithTheName_IsNotAMatch()
    {
        Directory.CreateDirectory(Path.Combine(_first, "fake-tool"));
        Directory.CreateDirectory(Path.Combine(_first, "fake-tool.exe"));

        Assert.Multiple(() =>
        {
            Assert.That(NonWindows(PathOf(_first)).Find("fake-tool"), Is.Null);
            Assert.That(Windows(PathOf(_first), ".exe").Find("fake-tool"), Is.Null);
        });
    }

    [Test]
    public void Find_NameWithDirectoryPart_SearchesOnlyThatDirectory()
    {
        string tool = CreateFile(_first, "fake-tool");
        var locator = NonWindows(PathOf(_second));

        Assert.Multiple(() =>
        {
            Assert.That(locator.Find(Path.Combine(_first, "fake-tool")), Is.EqualTo(tool));
            Assert.That(locator.Find(Path.Combine(_second, "fake-tool")), Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    public void Find_PathUnsetOrEmpty_ReturnsNull(string? path)
    {
        CreateFile(_first, "fake-tool");

        Assert.That(NonWindows(path).Find("fake-tool"), Is.Null);
    }

    [Test]
    public void Find_EmptyPathEntriesAreSkipped()
    {
        string tool = CreateFile(_first, "fake-tool");
        string path = Path.PathSeparator + _first + Path.PathSeparator + Path.PathSeparator;

        Assert.That(NonWindows(path).Find("fake-tool"), Is.EqualTo(tool));
    }

    [Test]
    public void Find_ReadsOnlyPathAndPathExtVariables()
    {
        var requested = new List<string>();
        var locator = new PathExecutableLocator(
            name =>
            {
                requested.Add(name);
                return name == "PATH" ? _first : null;
            },
            isWindows: true);

        locator.Find("fake-tool");

        Assert.That(requested, Is.EquivalentTo(PathAndPathExt));
    }

    [Test]
    public void Find_EmptyName_Throws()
    {
        Assert.That(() => NonWindows(PathOf(_first)).Find(string.Empty), Throws.ArgumentException);
    }

    [Test]
    public void DefaultConstructor_UnknownExecutable_ReturnsNull()
    {
        var locator = new PathExecutableLocator();

        Assert.That(locator.Find("no-such-tool-" + Guid.NewGuid().ToString("N")), Is.Null);
    }

    [Test]
    public void Constructor_NullEnvironment_Throws()
    {
        Assert.That(() => new PathExecutableLocator(null!, isWindows: false), Throws.ArgumentNullException);
    }

    private static string PathOf(params string[] directories) => string.Join(Path.PathSeparator, directories);

    private static PathExecutableLocator Windows(string? path, string? pathExt) =>
        new(name => name switch
        {
            "PATH" => path,
            "PATHEXT" => pathExt,
            _ => null,
        }, isWindows: true);

    private static PathExecutableLocator NonWindows(string? path) =>
        new(name => name == "PATH" ? path : null, isWindows: false);

    private static string CreateFile(string directory, string name)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllBytes(path, []);
        return path;
    }
}
