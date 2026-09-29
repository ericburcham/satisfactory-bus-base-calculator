namespace Satisfactory.Planner.Core.Identifiers;

/// <summary>
/// Strongly typed id of a schematic (milestone, MAM research, alternate hard drive, ...): its key in the data file's <c>schematics</c> object
/// (for example <c>Schematic_1-1_C</c>). Equality and ordering are ordinal.
/// </summary>
/// <param name="Value">The class name.</param>
public readonly record struct SchematicId(string Value) : IComparable<SchematicId>
{
    /// <summary>The class name (never <see langword="null"/> for a constructed id).</summary>
    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));

    /// <summary>Compares two ids ordinally by <see cref="Value"/>.</summary>
    /// <param name="other">The id to compare with.</param>
    /// <returns>A negative number, zero or a positive number, as <see cref="string.CompareOrdinal(string, string)"/>.</returns>
    public int CompareTo(SchematicId other) => string.CompareOrdinal(Value, other.Value);

    /// <summary>Returns <see cref="Value"/>.</summary>
    /// <returns>The class name.</returns>
    public override string ToString() => Value;

    /// <summary>Ordinal "less than".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts before <paramref name="right"/>.</returns>
    public static bool operator <(SchematicId left, SchematicId right) => left.CompareTo(right) < 0;

    /// <summary>Ordinal "less than or equal".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort after <paramref name="right"/>.</returns>
    public static bool operator <=(SchematicId left, SchematicId right) => left.CompareTo(right) <= 0;

    /// <summary>Ordinal "greater than".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> sorts after <paramref name="right"/>.</returns>
    public static bool operator >(SchematicId left, SchematicId right) => left.CompareTo(right) > 0;

    /// <summary>Ordinal "greater than or equal".</summary>
    /// <param name="left">The left id.</param>
    /// <param name="right">The right id.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/> does not sort before <paramref name="right"/>.</returns>
    public static bool operator >=(SchematicId left, SchematicId right) => left.CompareTo(right) >= 0;
}
