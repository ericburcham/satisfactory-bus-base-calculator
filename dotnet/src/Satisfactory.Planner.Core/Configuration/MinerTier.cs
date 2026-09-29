namespace Satisfactory.Planner.Core.Configuration;

/// <summary>The miner used for every solid resource node (Python <c>--miner</c>).</summary>
public enum MinerTier
{
    /// <summary>Miner Mk.1 (<c>mk1</c>): 60/min on a normal node, 5 MW.</summary>
    Mk1,

    /// <summary>Miner Mk.2 (<c>mk2</c>): 120/min on a normal node, 15 MW.</summary>
    Mk2,

    /// <summary>Miner Mk.3 (<c>mk3</c>): 240/min on a normal node, 45 MW.</summary>
    Mk3,
}
