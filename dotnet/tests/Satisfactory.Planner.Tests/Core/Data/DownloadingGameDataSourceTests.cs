using System.Net;
using Satisfactory.Planner.Core;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Model;
using Satisfactory.Planner.Tests.TestSupport;

namespace Satisfactory.Planner.Tests.Core.Data;

[TestFixture]
public sealed class DownloadingGameDataSourceTests
{
    private CoreScratchDirectory _scratch = null!;

    [SetUp]
    public void SetUp() => _scratch = new CoreScratchDirectory();

    [TearDown]
    public void TearDown() => _scratch.Dispose();

    [Test]
    public async Task MissingFile_IsDownloadedIntoANewFolderThenLoaded()
    {
        string path = _scratch.Combine("nested", "deeper", "satisfactory_data.json");
        using var handler = new FakeHttpMessageHandler("{\"downloaded\": true}");
        using var http = new HttpClient(handler);
        var inner = new RecordingSource(path);
        using var notices = new StringWriter { NewLine = "\n" };
        var source = new DownloadingGameDataSource(inner, http, path, notices);

        GameData data = await source.LoadAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.Requests[0], Is.EqualTo(new Uri(PlannerConstants.DataUrl)));
            Assert.That(notices.ToString(), Is.EqualTo($"{path} not found; downloading {PlannerConstants.DataUrl}\n"));
            Assert.That(File.ReadAllText(path), Is.EqualTo("{\"downloaded\": true}"));
            Assert.That(inner.Calls, Is.EqualTo(1));
            Assert.That(inner.FileExistedWhenCalled, Is.True);
            Assert.That(data, Is.SameAs(inner.Result));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!), Has.Length.EqualTo(1), "no partial file left");
        });
    }

    [Test]
    public void Notice_UsesThePathAsGiven()
    {
        // Python prints f"{path} not found; downloading {DATA_URL}" with the path the user passed.
        string path = _scratch.Combine("data.json");
        using var handler = new FakeHttpMessageHandler("{}");
        using var http = new HttpClient(handler);
        using var notices = new StringWriter();
        var source = new DownloadingGameDataSource(new RecordingSource(path), http, path, notices);

        source.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        Assert.That(
            notices.ToString(),
            Is.EqualTo(path + " not found; downloading https://raw.githubusercontent.com/greeny/SatisfactoryTools/dev/data/data.json"
                       + Environment.NewLine));
    }

    [Test]
    public async Task ExistingFile_IsNotDownloaded()
    {
        string path = _scratch.WriteFile("satisfactory_data.json", "{\"local\": true}");
        using var handler = new FakeHttpMessageHandler("{\"downloaded\": true}");
        using var http = new HttpClient(handler);
        var inner = new RecordingSource(path);
        using var notices = new StringWriter();
        var source = new DownloadingGameDataSource(inner, http, path, notices);

        await source.LoadAsync(CancellationToken.None);
        await source.LoadAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.CallCount, Is.Zero);
            Assert.That(notices.ToString(), Is.Empty);
            Assert.That(File.ReadAllText(path), Is.EqualTo("{\"local\": true}"));
            Assert.That(inner.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task SecondLoad_AfterADownload_DoesNotDownloadAgain()
    {
        string path = _scratch.Combine("satisfactory_data.json");
        using var handler = new FakeHttpMessageHandler("{}");
        using var http = new HttpClient(handler);
        using var notices = new StringWriter();
        var source = new DownloadingGameDataSource(new RecordingSource(path), http, path, notices);

        await source.LoadAsync(CancellationToken.None);
        await source.LoadAsync(CancellationToken.None);

        Assert.That(handler.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedDownload_ThrowsPlannerExceptionAndLeavesNoFile()
    {
        string path = _scratch.Combine("satisfactory_data.json");
        using var handler = new FakeHttpMessageHandler("not found", HttpStatusCode.NotFound);
        using var http = new HttpClient(handler);
        var inner = new RecordingSource(path);
        using var notices = new StringWriter();
        var source = new DownloadingGameDataSource(inner, http, path, notices);

        Assert.That(
            () => source.LoadAsync(CancellationToken.None),
            Throws.TypeOf<PlannerException>().With.Message.Contains(PlannerConstants.DataUrl));
        Assert.Multiple(() =>
        {
            Assert.That(Directory.GetFiles(_scratch.Path), Is.Empty);
            Assert.That(inner.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task DownloadedFile_IsReadableByTheJsonSource()
    {
        string path = _scratch.Combine("fresh", "satisfactory_data.json");
        const string json = """
            { "items": { "Desc_NuclearFuelRod_C": { "name": "U", "energyValue": 750000.0 },
                         "Desc_PlutoniumFuelRod_C": { "name": "P", "energyValue": 1500000.0 } },
              "recipes": {}, "schematics": {}, "resources": { "Desc_OreIron_C": {} }, "buildings": {} }
            """;
        using var handler = new FakeHttpMessageHandler(json);
        using var http = new HttpClient(handler);
        using var notices = new StringWriter();
        var source = new DownloadingGameDataSource(new JsonGameDataSource(path), http, path, notices);

        GameData data = await source.LoadAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(data.Resources.Select(r => r.Value), Is.EqualTo(ExpectedSequence.Of("Desc_OreIron_C")));
            Assert.That(data.Recipes, Has.Count.EqualTo(2));
        });
    }

    private sealed class RecordingSource(string path) : IGameDataSource
    {
        public int Calls { get; private set; }

        public bool FileExistedWhenCalled { get; private set; }

        public GameData Result { get; } = new GameData([], [], [], [], [], []);

        public Task<GameData> LoadAsync(CancellationToken cancellationToken)
        {
            Calls++;
            FileExistedWhenCalled = File.Exists(path);
            return Task.FromResult(Result);
        }
    }
}
