namespace Satisfactory.Planner.Core;

/// <summary>
/// Base class for every planner failure that the Python reference reports with <c>sys.exit(message)</c>
/// (or that the port reports in the same way). The command line prints <see cref="Exception.Message"/> and exits with 1.
/// </summary>
public class PlannerException : Exception
{
    /// <summary>Creates an exception with a default message.</summary>
    public PlannerException()
    {
    }

    /// <summary>Creates an exception with the given message.</summary>
    /// <param name="message">The message shown to the user.</param>
    public PlannerException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with the given message and the exception that caused it.</summary>
    /// <param name="message">The message shown to the user.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public PlannerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
