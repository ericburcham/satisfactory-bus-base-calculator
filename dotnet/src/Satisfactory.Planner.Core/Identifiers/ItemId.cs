namespace Satisfactory.Planner.Core.Identifiers;

/// <summary>
/// Strongly typed id of an item: the item's class name, i.e. its key in the data file's <c>items</c> object
/// (for example <c>Desc_IronPlate_C</c>). Equality and ordering are ordinal.
/// </summary>
/// <param name="Value">The class name.</param>
public readonly record struct ItemId(string Value) : IComparable<ItemId>
{
    /// <summary>The class name (never <see langword="null"/> for a constructed id).</summary>
    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));

    /// <summary>Compares two ids ordinally by <see cref="Value"/>.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns>A negative number, zero or a positive number, as <see cref="string.CompareOrdinal(string, string)"/>.</returns>
    public int CompareTo(ItemId other) => string.CompareOrdinal(Value, other.Value);

    /// <summary>Returns <see cref="Value"/>.</summary>
    /// <returns>The class name.</returns>
    public override string ToString() => Value;

    /// <summary>Ordinal "less than".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(ItemId left, ItemId right) => left.CompareTo(right) < 0;

    /// <summary>Ordinal "less than or equal".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(ItemId left, ItemId right) => left.CompareTo(right) <= 0;

    /// <summary>Ordinal "greater than".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(ItemId left, ItemId right) => left.CompareTo(right) > 0;

    /// <summary>Ordinal "greater than or equal".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(ItemId left, ItemId right) => left.CompareTo(right) >= 0;
}
