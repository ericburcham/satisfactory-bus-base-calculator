using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Extraction;
using Satisfactory.Planner.Core.Progression;
using Satisfactory.Planner.Core.Selection;

namespace Satisfactory.Planner.Core.Solving;

/// <summary>Everything one solve depends on besides the excess targets.</summary>
/// <param name="Tier">The tier.</param>
/// <param name="Choice">The recipe chosen for each item (Python <c>recipe_for</c>).</param>
/// <param name="Credit">The round's credit policy.</param>
/// <param name="Existing">The machines built in earlier tiers; their spare capacity is used first.</param>
/// <param name="Extractors">The extractors (Python <c>extractor_for</c>), normally the tier's.</param>
/// <param name="DisposalFinder">Finds the recipe that disposes of a leftover fluid.</param>
public sealed record SolveContext(
    ITierContext Tier,
    RecipeChoice Choice,
    CreditPolicy Credit,
    IExistingCapacity Existing,
    IExtractorCatalog Extractors,
    IFluidDisposalFinder DisposalFinder);
