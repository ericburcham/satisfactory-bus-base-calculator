using System.Collections.Frozen;
using Satisfactory.Planner.Core.Configuration;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Extraction;

/// <summary>
/// Python <c>extractor_for</c>: a locked resource has no extractor; a solid resource uses the chosen miner at the chosen
/// purity; water, crude oil and nitrogen use their fixed extractors; anything else has none.
/// </summary>
public sealed class ExtractorCatalog : IExtractorCatalog
{
    private readonly ExtractorSpec _miner;
    private readonly FrozenSet<ItemId> _locked;

    /// <summary>Creates the catalog.</summary>
    /// <param name="miner">The miner for solid resources.</param>
    /// <param name="purity">The node purity for solid resources.</param>
    /// <param name="lockedRawResources">Resources whose extractor is not unlocked yet (copied).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="miner"/> or <paramref name="purity"/> is not defined.</exception>
    public ExtractorCatalog(MinerTier miner, NodePurity purity, IReadOnlySet<ItemId> lockedRawResources)
    {
        ArgumentNullException.ThrowIfNull(lockedRawResources);
        if (!PlannerConstants.Miners.TryGetValue(miner, out ExtractorSpec? spec))
        {
            throw new ArgumentOutOfRangeException(nameof(miner), miner, "Unknown miner tier.");
        }

        if (!PlannerConstants.PurityMultipliers.TryGetValue(purity, out double multiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(purity), purity, "Unknown node purity.");
        }

        _miner = new ExtractorSpec(spec.Name, spec.RatePerMinute * multiplier, spec.PowerMw);
        _locked = lockedRawResources.ToFrozenSet();
    }

    /// <inheritdoc />
    public ExtractorSpec? For(ItemId resource)
    {
        if (_locked.Contains(resource))
        {
            return null;
        }

        if (PlannerConstants.SolidResources.Contains(resource))
        {
            return _miner;
        }

        if (resource == PlannerConstants.Water)
        {
            return PlannerConstants.WaterExtractor;
        }

        if (resource == PlannerConstants.CrudeOil)
        {
            return PlannerConstants.OilExtractor;
        }

        if (resource == PlannerConstants.NitrogenGas)
        {
            return PlannerConstants.NitrogenWell;
        }

        return null;
    }

    /// <inheritdoc />
    public bool IsExtractable(ItemId item) => For(item) is not null;
}
