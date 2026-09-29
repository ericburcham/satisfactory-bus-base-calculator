using System.Diagnostics.CodeAnalysis;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Core.Extraction;

/// <summary>Knows which natural resources can be extracted, and with what (Python <c>extractor_for</c>).</summary>
public interface IExtractorCatalog
{
    /// <summary>The extractor for a resource, or <see langword="null"/> when it cannot be extracted (yet).</summary>
    /// <param name="resource">The item.</param>
    /// <returns>The extractor spec, or <see langword="null"/>.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The member name For is fixed by the shared port spec; the solution is C# only.")]
    ExtractorSpec? For(ItemId resource);

    /// <summary>Whether <see cref="For"/> returns an extractor for the item.</summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> when the item can be extracted.</returns>
    bool IsExtractable(ItemId item);
}
