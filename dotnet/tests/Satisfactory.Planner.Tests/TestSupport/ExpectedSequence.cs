namespace Satisfactory.Planner.Tests.TestSupport;

/// <summary>
/// Builds expected collections for assertions without constant array arguments (analyzer CA1861):
/// <c>Is.EqualTo(ExpectedSequence.Of("a", "b"))</c>.
/// </summary>
public static class ExpectedSequence
{
    /// <summary>Returns the items as an array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The expected items, in order.</param>
    /// <returns><paramref name="items"/>.</returns>
    public static T[] Of<T>(params T[] items) => items;
}
