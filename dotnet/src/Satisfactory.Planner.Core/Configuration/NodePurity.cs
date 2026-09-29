namespace Satisfactory.Planner.Core.Configuration;

/// <summary>The purity assumed for every solid resource node (Python <c>--purity</c>).</summary>
public enum NodePurity
{
    /// <summary>Impure (<c>impure</c>): half the miner's normal rate.</summary>
    Impure,

    /// <summary>Normal (<c>normal</c>): the miner's normal rate.</summary>
    Normal,

    /// <summary>Pure (<c>pure</c>): twice the miner's normal rate.</summary>
    Pure,
}
