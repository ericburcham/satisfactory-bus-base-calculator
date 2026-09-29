namespace Satisfactory.Planner.Core.Data;

/// <summary>
/// The game data is not usable: the file is not valid JSON, a field the Python reads directly is missing or has the
/// wrong type, or a well-known id (such as a fuel rod item or a recipe's building) is unknown.
/// </summary>
public sealed class GameDataFormatException : PlannerException
{
    /// <summary>Creates an exception with a default message.</summary>
    public GameDataFormatException()
    {
    }

    /// <summary>Creates an exception with the given message.</summary>
    /// <param name="message">The message, naming the field and the object.</param>
    public GameDataFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with the given message and the exception that caused it.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public GameDataFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
