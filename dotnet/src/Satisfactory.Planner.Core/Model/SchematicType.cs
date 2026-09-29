namespace Satisfactory.Planner.Core.Model;

/// <summary>The <c>type</c> of a schematic in the data file.</summary>
public enum SchematicType
{
    /// <summary><c>EST_Milestone</c>: a HUB milestone; it carries its own tier.</summary>
    Milestone,

    /// <summary><c>EST_Tutorial</c>: an onboarding step; it carries its own tier.</summary>
    Tutorial,

    /// <summary><c>EST_MAM</c>: MAM research; it needs the MAM.</summary>
    Mam,

    /// <summary><c>EST_Alternate</c>: an alternate recipe from a hard drive; it needs the MAM.</summary>
    Alternate,

    /// <summary><c>EST_HardDrive</c>: hard drive research; it needs the MAM.</summary>
    HardDrive,

    /// <summary><c>EST_ResourceSink</c>: an AWESOME Shop purchase (ignored by the tier computation).</summary>
    ResourceSink,

    /// <summary><c>EST_Customization</c>: a customization unlock (ignored by the tier computation).</summary>
    Customization,

    /// <summary>Any other type (ignored by the tier computation).</summary>
    Other,
}
