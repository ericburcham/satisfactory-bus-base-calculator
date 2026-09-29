using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>The extraction of one raw resource (Python row with <c>kind="extractor"</c>).</summary>
/// <param name="Resource">The resource.</param>
/// <param name="Spec">Its extractor.</param>
/// <param name="RatePerMinute">The extracted rate per minute (Python <c>rate</c>).</param>
public sealed record ExtractorRow(ItemId Resource, ExtractorSpec Spec, double RatePerMinute);
