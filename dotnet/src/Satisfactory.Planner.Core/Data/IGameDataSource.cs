using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Data;

/// <summary>Supplies the game data (Python <c>load_data</c> plus the recipe setup of <c>Planner.__init__</c>).</summary>
public interface IGameDataSource
{
    /// <summary>Loads the game data.</summary>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>The game data.</returns>
    Task<GameData> LoadAsync(CancellationToken cancellationToken);
}
