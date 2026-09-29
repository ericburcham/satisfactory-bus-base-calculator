namespace Satisfactory.Planner.Reporting.Formatting;

/// <summary>
/// Python string format specs <c>{s:30s}</c> (left-aligned) and <c>{s:>9s}</c> (right-aligned).
/// </summary>
/// <remarks>
/// Like Python, the width counts code points (a surrogate pair is one character) and text longer than the width is
/// never truncated.
/// </remarks>
public static class PythonTextFormat
{
    /// <summary>Python <c>f"{text:{width}s}"</c>: pads with spaces on the right to <paramref name="width"/>.</summary>
    /// <param name="text">The text to pad.</param>
    /// <param name="width">The minimum width; 0 or less means no padding.</param>
    /// <returns>The padded text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public static string PadRight(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);
        int padding = width - CodePointLength(text);
        return padding > 0 ? text + new string(' ', padding) : text;
    }

    /// <summary>Python <c>f"{text:>{width}s}"</c>: pads with spaces on the left to <paramref name="width"/>.</summary>
    /// <param name="text">The text to pad.</param>
    /// <param name="width">The minimum width; 0 or less means no padding.</param>
    /// <returns>The padded text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public static string PadLeft(string text, int width)
    {
        ArgumentNullException.ThrowIfNull(text);
        int padding = width - CodePointLength(text);
        return padding > 0 ? new string(' ', padding) + text : text;
    }

    /// <summary>Python <c>len(text)</c>: the number of code points.</summary>
    private static int CodePointLength(string text)
    {
        int length = text.Length;
        for (int index = 1; index < text.Length; index++)
        {
            if (char.IsSurrogatePair(text[index - 1], text[index]))
            {
                length--;
                index++;
            }
        }

        return length;
    }
}
