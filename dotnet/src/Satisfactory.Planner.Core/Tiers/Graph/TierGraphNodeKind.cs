namespace Satisfactory.Planner.Core.Tiers.Graph;

/// <summary>The kinds of node in a tier graph.</summary>
public enum TierGraphNodeKind
{
    /// <summary>A group of identical new machines running one recipe.</summary>
    Machine,

    /// <summary>A raw resource taken from extractors.</summary>
    RawInput,

    /// <summary>An item taken from the main bus (made in an earlier tier).</summary>
    BusInput,

    /// <summary>The AWESOME Sink.</summary>
    Sink,
}
