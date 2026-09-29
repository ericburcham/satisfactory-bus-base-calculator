using Satisfactory.Planner.Core.Data;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>The real <c>data/satisfactory_data.json</c>, loaded once per test run with <see cref="JsonGameDataSource"/>.</summary>
public static class CoreRealGameData
{
    private static readonly Lazy<GameData> LazyData = new(
        () => new JsonGameDataSource(RepositoryPaths.DataFile).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

    /// <summary>The loaded game data.</summary>
    public static GameData Data => LazyData.Value;
}
