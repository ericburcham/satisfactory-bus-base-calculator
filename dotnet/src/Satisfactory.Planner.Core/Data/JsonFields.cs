using System.Globalization;
using System.Text.Json;

namespace Satisfactory.Planner.Core.Data;

/// <summary>
/// Typed access to JSON fields that reports a missing or mistyped field as a <see cref="GameDataFormatException"/>
/// naming the field and the object it belongs to.
/// </summary>
internal static class JsonFields
{
    /// <summary>
    /// The properties of an object as Python's <c>json.load</c> keeps them: document order, and for a duplicate key the
    /// position of the first occurrence with the value of the last.
    /// </summary>
    public static OrderedDictionary<string, JsonElement> Properties(JsonElement element, string context)
    {
        RequireKind(element, JsonValueKind.Object, context);
        var properties = new OrderedDictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            properties[property.Name] = property.Value;
        }

        return properties;
    }

    public static JsonElement Required(JsonElement obj, string name, string context)
    {
        RequireKind(obj, JsonValueKind.Object, context);
        if (!TryGetLast(obj, name, out JsonElement value))
        {
            throw new GameDataFormatException($"Missing required field '{name}' in {context}.");
        }

        return value;
    }

    public static bool TryGet(JsonElement obj, string name, string context, out JsonElement value)
    {
        RequireKind(obj, JsonValueKind.Object, context);
        return TryGetLast(obj, name, out value);
    }

    public static string RequiredString(JsonElement obj, string name, string context) =>
        AsString(Required(obj, name, context), name, context);

    public static string? OptionalString(JsonElement obj, string name, string context) =>
        TryGet(obj, name, context, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? AsString(value, name, context)
            : null;

    public static double RequiredDouble(JsonElement obj, string name, string context) =>
        AsDouble(Required(obj, name, context), name, context);

    public static double OptionalDouble(JsonElement obj, string name, string context, double fallback) =>
        TryGet(obj, name, context, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? AsDouble(value, name, context)
            : fallback;

    public static int RequiredInt(JsonElement obj, string name, string context) =>
        AsInt(Required(obj, name, context), name, context);

    public static int OptionalInt(JsonElement obj, string name, string context, int fallback) =>
        TryGet(obj, name, context, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? AsInt(value, name, context)
            : fallback;

    public static bool RequiredBool(JsonElement obj, string name, string context) =>
        AsBool(Required(obj, name, context), name, context);

    public static bool OptionalBool(JsonElement obj, string name, string context, bool fallback) =>
        TryGet(obj, name, context, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? AsBool(value, name, context)
            : fallback;

    public static JsonElement RequiredArray(JsonElement obj, string name, string context)
    {
        JsonElement value = Required(obj, name, context);
        RequireFieldKind(value, JsonValueKind.Array, name, context);
        return value;
    }

    public static JsonElement? OptionalArray(JsonElement obj, string name, string context)
    {
        if (!TryGet(obj, name, context, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireFieldKind(value, JsonValueKind.Array, name, context);
        return value;
    }

    public static JsonElement RequiredObject(JsonElement obj, string name, string context)
    {
        JsonElement value = Required(obj, name, context);
        RequireFieldKind(value, JsonValueKind.Object, name, context);
        return value;
    }

    public static string AsString(JsonElement value, string name, string context)
    {
        RequireFieldKind(value, JsonValueKind.String, name, context);
        return value.GetString()!;
    }

    /// <summary>
    /// Whether a JSON number token is an integer literal, i.e. one that Python's <c>json.load</c> turns into an
    /// <c>int</c>: it has no fraction and no exponent.
    /// </summary>
    public static bool IsIntegerLiteral(JsonElement number)
    {
        string raw = number.GetRawText();
        return raw.IndexOfAny(['.', 'e', 'E']) < 0;
    }

    private static double AsDouble(JsonElement value, string name, string context)
    {
        RequireFieldKind(value, JsonValueKind.Number, name, context);
        return value.GetDouble();
    }

    private static int AsInt(JsonElement value, string name, string context)
    {
        RequireFieldKind(value, JsonValueKind.Number, name, context);
        if (value.TryGetInt32(out int result))
        {
            return result;
        }

        double number = value.GetDouble();
        if (IsIntegerLiteral(value) || number != Math.Floor(number) || number < int.MinValue || number > int.MaxValue)
        {
            throw new GameDataFormatException(
                $"Field '{name}' in {context} must be an integer, but is {value.GetRawText()}.");
        }

        return (int)number;
    }

    private static bool AsBool(JsonElement value, string name, string context) =>
        value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw WrongKind(value, "boolean", name, context),
        };

    private static bool TryGetLast(JsonElement obj, string name, out JsonElement value)
    {
        bool found = false;
        value = default;
        foreach (JsonProperty property in obj.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                value = property.Value;
                found = true;
            }
        }

        return found;
    }

    private static void RequireKind(JsonElement element, JsonValueKind kind, string context)
    {
        if (element.ValueKind != kind)
        {
            throw new GameDataFormatException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{context} must be a JSON {KindName(kind)}, but is {KindName(element.ValueKind)}."));
        }
    }

    private static void RequireFieldKind(JsonElement value, JsonValueKind kind, string name, string context)
    {
        if (value.ValueKind != kind)
        {
            throw WrongKind(value, KindName(kind), name, context);
        }
    }

    private static GameDataFormatException WrongKind(JsonElement value, string expected, string name, string context) =>
        new($"Field '{name}' in {context} must be a {expected}, but is {KindName(value.ValueKind)}.");

    private static string KindName(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Null => "null",
        _ => "undefined",
    };
}
