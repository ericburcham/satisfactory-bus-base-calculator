using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;
using Satisfactory.Planner.Core.Model;

namespace Satisfactory.Planner.Core.Progression;

/// <summary>
/// Factory for <see cref="TierContext"/> (Python <c>set_tier</c>): a resource is locked while its extractor building's
/// tier is above the tier; a recipe is available once its <see cref="IUnlockTierResolver.RecipeTier"/> is reached.
/// </summary>
public sealed class TierContextFactory : ITierContextFactory
{
    private readonly GameData _data;
    private readonly IUnlockTierResolver _resolver;
    private readonly MinerTier _miner;
    private readonly NodePurity _purity;

    /// <summary>Creates the factory.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="resolver">The unlock tiers.</param>
    /// <param name="miner">The miner for solid resources.</param>
    /// <param name="purity">The node purity for solid resources.</param>
    public TierContextFactory(GameData data, IUnlockTierResolver resolver, MinerTier miner, NodePurity purity)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(resolver);
        _data = data;
        _resolver = resolver;
        _miner = miner;
        _purity = purity;
    }

    /// <inheritdoc />
    public ITierContext Create(int tier)
    {
        var locked = new HashSet<ItemId>(_data.Resources.Where(c => _resolver.RawResourceTier(c) > tier));
        var extractors = new ExtractorCatalog(_miner, _purity, locked);
        IEnumerable<Recipe> available = _data.Recipes.Where(r => _resolver.RecipeTier(r) <= tier);
        return new TierContext(tier, _data, available, locked, extractors);
    }
}
