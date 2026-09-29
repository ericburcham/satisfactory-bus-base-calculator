using System.Buffers;
using System.Text;

namespace Satisfactory.Planner.Reporting.Formatting;

/// <summary>
/// CSV exactly as Python's <c>csv.writer</c> with the default <c>excel</c> dialect writes it: delimiter <c>','</c>,
/// quote character <c>'"'</c> (doubled inside quoted fields), <c>QUOTE_MINIMAL</c> and <c>"\r\n"</c> after every
/// row.
/// </summary>
/// <remarks>
/// A field is quoted only when it contains <c>','</c>, <c>'"'</c>, <c>'\r'</c> or <c>'\n'</c>; leading or trailing
/// spaces and tabs are written as they are. A row that consists of a single empty field is written as <c>""</c>
/// (so that it is not read back as an empty row); an empty field among others is written as nothing. A
/// <see langword="null"/> field is written like Python's <c>None</c>, as an empty field.
/// </remarks>
public sealed class CsvFormatter : ICsvFormatter
{
    private const char Delimiter = ',';
    private const char QuoteChar = '"';
    private const string LineTerminator = "\r\n";
    private static readonly SearchValues<char> CharactersNeedingQuotes = SearchValues.Create(",\"\r\n");

    /// <inheritdoc />
    public string FormatRow(IReadOnlyList<string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var text = new StringBuilder();
        AppendRow(text, fields);
        return text.ToString();
    }

    /// <inheritdoc />
    public string Format(IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var text = new StringBuilder();
        foreach (IReadOnlyList<string> row in rows)
        {
            ArgumentNullException.ThrowIfNull(row, nameof(rows));
            AppendRow(text, row);
        }

        return text.ToString();
    }

    private static void AppendRow(StringBuilder text, IReadOnlyList<string> fields)
    {
        if (fields.Count == 1 && string.IsNullOrEmpty(fields[0]))
        {
            text.Append(QuoteChar).Append(QuoteChar).Append(LineTerminator);
            return;
        }

        for (int index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                text.Append(Delimiter);
            }

            AppendField(text, fields[index] ?? string.Empty);
        }

        text.Append(LineTerminator);
    }

    private static void AppendField(StringBuilder text, string field)
    {
        if (!field.AsSpan().ContainsAny(CharactersNeedingQuotes))
        {
            text.Append(field);
            return;
        }

        text.Append(QuoteChar);
        foreach (char c in field)
        {
            if (c == QuoteChar)
            {
                text.Append(QuoteChar);
            }

            text.Append(c);
        }

        text.Append(QuoteChar);
    }
}
