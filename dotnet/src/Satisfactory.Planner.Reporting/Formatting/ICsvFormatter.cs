namespace Satisfactory.Planner.Reporting.Formatting;

/// <summary>Formats rows of text fields as CSV.</summary>
public interface ICsvFormatter
{
    /// <summary>Formats one row, including its line terminator.</summary>
    /// <param name="fields">The fields of the row, in column order.</param>
    /// <returns>The CSV text of the row, terminated.</returns>
    string FormatRow(IReadOnlyList<string> fields);

    /// <summary>Formats several rows; every row, including the last, ends with the line terminator.</summary>
    /// <param name="rows">The rows, in order.</param>
    /// <returns>The CSV text of all rows.</returns>
    string Format(IEnumerable<IReadOnlyList<string>> rows);
}
