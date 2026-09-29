namespace Satisfactory.Planner.Core.Extraction;

/// <summary>An extractor for a natural resource (Python's <c>(name, rate, mw)</c> tuples).</summary>
/// <param name="Name">The extractor's display name.</param>
/// <param name="RatePerMinute">Units (or m3) per minute of one extractor at 100% clock.</param>
/// <param name="PowerMw">The power one extractor consumes, in MW.</param>
public sealed record ExtractorSpec(string Name, double RatePerMinute, double PowerMw);
